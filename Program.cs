﻿using IORPG.Game;
using IORPG.Game.Mutations;
using IORPG.Networking;
using SharpMath2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace IORPG
{
    public class Program
    {
        public const int GAME_SPEED_MULTIPLIER = 2;

        public static void Main(string[] args)
        {
            Console.WriteLine("====IORPG Server====");
            Console.WriteLine();
            Console.WriteLine("Starting logic thread...");
            ThreadStart logicThreadSt = new ThreadStart(StartLogicThread);
            Thread logicThread = new Thread(logicThreadSt);
            logicThread.Start();

            Console.WriteLine("Awaiting logic threads initialization...");
            int counter = 0;
            while(State == null || QueuedMutations == null || RecentTickTimes == null)
            {
                counter++;
                Thread.Sleep(100);
                if (counter % 5 == 0)
                    Console.WriteLine("...");
            }
            Console.WriteLine("Logic thread initialization successful");

            Console.WriteLine("Starting networking thread...");
            ThreadStart networkingThreadSt = new ThreadStart(StartMainNetworkingThread);
            Thread networkingThread = new Thread(networkingThreadSt);
            networkingThread.Start();

            Console.WriteLine("All Threads Started");
            Console.WriteLine("-----------------------------");
            Console.WriteLine();
            while (true)
            {
                Console.WriteLine("Options:");
                Console.WriteLine("  [c]heck threads, [s]top threads, get [t]ick rate, suppress [l]ong tick message");
                Console.WriteLine("  c[h]ange tick rate");
                var key = Console.ReadKey();
                Console.WriteLine();
                switch(key.KeyChar)
                {
                    case 'c':
                        Console.WriteLine($"Logic Thread:      {logicThread.ThreadState.ToString()}");
                        Console.WriteLine($"Networking Thread: {networkingThread.ThreadState.ToString()}");
                        break;
                    case 's':
                        Console.WriteLine("Requesting stop...");
                        StopRequested = true;

                        bool logicDone = false, netDone = false;
                        while(!logicDone || !netDone)
                        {
                            if(!logicDone && logicThread.ThreadState == System.Threading.ThreadState.Stopped)
                            {
                                Console.WriteLine("Logic thread shutdown successful");
                                logicDone = true;
                            }

                            if(!netDone && networkingThread.ThreadState == System.Threading.ThreadState.Stopped)
                            {
                                Console.WriteLine("Networking thread shutdown successful");
                                netDone = true;
                            }

                            Thread.Sleep(500);
                            Console.WriteLine("...");
                        }
                        Console.WriteLine("All threads stopped");
                        Console.WriteLine();
                        Console.WriteLine("Press any key to exit");
                        Console.ReadKey();
                        return;
                    case 't':
                        var world = State.World;
                        int tickSpeed = RecentTickTimesSum / RecentTickTimes.Count;

                        int totalNumNearby = 0;
                        foreach(var e in world.Entities)
                        {
                            totalNumNearby += e.NearbyEntityIds.Count;
                        }
                        float avgNearby = world.Entities.Count == 0 ? 0 : (totalNumNearby / world.Entities.Count);
                        Console.WriteLine("==Logic Thread Tick Rate==");
                        Console.WriteLine($"Time per tick: ~{tickSpeed}ms/tick");
                        Console.WriteLine($"Behind by ~{MissedUpdateMS}ms");
                        Console.WriteLine($"There are {world.Entities.Count} entities");
                        Console.WriteLine($"On average, each entity has {avgNearby} nearby entities");
                        Console.WriteLine();
                        break;
                    case 'l':
                        SuppressLongTickMessage = !SuppressLongTickMessage;
                        Console.WriteLine($"SuppressLongTickMessages is now {SuppressLongTickMessage}");
                        break;
                    case 'h':
                        Console.WriteLine($"New desired tick rate? (Is currently {DesiredTickRate}ms/tick)");
                        var tickRateStr = Console.ReadLine();
                        int tickRate;
                        if(!int.TryParse(tickRateStr, out tickRate))
                        {
                            Console.WriteLine("Failed to parse string to int");
                            break;
                        }

                        Interlocked.Exchange(ref DesiredTickRate, tickRate);
                        Console.WriteLine($"Tick rate is now {DesiredTickRate}ms/tick");
                        break;
                    default:
                        Console.WriteLine("Unrecognized char!");
                        break;
                }
            }
        }

        private static int ShouldSendTick = 0;
        private static volatile int DesiredTickRate = 16;
        private static volatile bool StopRequested;
        private static volatile int MissedUpdateMS;
        private static ConcurrentQueue<int> RecentTickTimes;
        private static volatile int RecentTickTimesSum;
        private static volatile bool SuppressLongTickMessage;

        public static GameState State;
        public static ConcurrentDictionary<WorldMutationTime, ConcurrentQueue<IWorldMutation>> QueuedMutations;

        static void StartLogicThread()
        {
            var teams = new Dictionary<int, Team>{
                { 1, new Team(1, ImmutableHashSet.Create<int>(), new Rect2(50, 50, 1950, 4950), 0) },
                { 2, new Team(2, ImmutableHashSet.Create<int>(), new Rect2(3050, 50, 4950, 4950), 0) },
            };
            State = new GameState();
            State.World = new World(5000, 5000, 0, 0, new List<Entity>().AsReadOnly(), new ReadOnlyDictionary<int, int>(new Dictionary<int, int>()), new ReadOnlyDictionary<int, Team>(teams));
            QueuedMutations = new ConcurrentDictionary<WorldMutationTime, ConcurrentQueue<IWorldMutation>>();
            QueuedMutations.TryAdd(WorldMutationTime.AfterEntitiesTick, new ConcurrentQueue<IWorldMutation>());
            QueuedMutations.TryAdd(WorldMutationTime.BeforeEntitiesTick, new ConcurrentQueue<IWorldMutation>());
            RecentTickTimes = new ConcurrentQueue<int>();

            int considerChangeTickrateCounter = 0;
            var random = new Random();
            long lastWarningTime = 0;
            var watch = new Stopwatch();
            while(!StopRequested)
            {
                var desiredMs = DesiredTickRate;
                watch.Restart();
                var mut = Logic.SimulateTimePassing(State.World, random, QueuedMutations, desiredMs * GAME_SPEED_MULTIPLIER);
                State.World = mut.AsReadOnly();
                foreach(var cb in mut.FinishedCallbacks)
                {
                    cb?.Invoke();
                }
                watch.Stop();

                Interlocked.Exchange(ref ShouldSendTick, 1);
                int timeTaken = (int)watch.ElapsedMilliseconds;
                RecentTickTimes.Enqueue(timeTaken);
                RecentTickTimesSum += timeTaken;

                if(RecentTickTimes.Count > 1000)
                {
                    int popped;
                    RecentTickTimes.TryDequeue(out popped);
                    RecentTickTimesSum -= popped;
                }
                
                if(timeTaken > desiredMs)
                {

                    MissedUpdateMS = Math.Min(MissedUpdateMS + timeTaken - desiredMs, 1000);
                    considerChangeTickrateCounter++;

                    if(considerChangeTickrateCounter > 1000)
                    {
                        int tickSpeed = RecentTickTimesSum / RecentTickTimes.Count;

                        if(tickSpeed > DesiredTickRate)
                        {
                            Console.WriteLine($"WARNING: Average tick speed is greater than desired tick rate. Adjusting DesiredTickRate to {tickSpeed + 1}");
                            Interlocked.Exchange(ref DesiredTickRate, tickSpeed + 1);
                        }
                    }

                    if (!SuppressLongTickMessage)
                    {
                        var now = (DateTime.UtcNow.Ticks - 621355968000000000) / 10000;
                        if (now - lastWarningTime > 1000)
                        {
                            Console.WriteLine($"{DateTime.UtcNow} WARNING: Tick took too long (desired {desiredMs}ms but took {timeTaken}ms)");
                            lastWarningTime = now;
                        }
                    }
                    
                }else
                {
                    var sleepTime = desiredMs - timeTaken;

                    if(sleepTime <= MissedUpdateMS)
                    {
                        MissedUpdateMS -= sleepTime;
                    }else
                    {
                        sleepTime -= MissedUpdateMS;
                        MissedUpdateMS = 0;

                        watch.Restart();
                        Thread.Sleep(sleepTime);
                        watch.Stop();

                        var actualSleepTime = (int)watch.ElapsedMilliseconds;
                        if (actualSleepTime > sleepTime)
                            MissedUpdateMS = Math.Min(MissedUpdateMS + actualSleepTime - sleepTime, 1000);
                    }
                }
            }
        }

        static void StartMainNetworkingThread()
        {
            var wssv = new WebSocketServer(8081);
            wssv.AddWebSocketService<PlayService>("/Play");

            wssv.Start();
            if(!wssv.IsListening)
            {
                Console.WriteLine("Networking Thread: Failed to start");
                return;
            }

            var host = wssv.WebSocketServices["/Play"];
            int counter = 0;
            while (!StopRequested)
            {
                Thread.Sleep(100);
                counter++;
                if(counter >= 50)
                {
                    counter = 0;
                    host.Sessions.Sweep();
                }

                if (Interlocked.CompareExchange(ref ShouldSendTick, 0, 1) == 1)
                {
                    var world = State.World;
                    foreach (var sess in host.Sessions.Sessions)
                    {
                        var playSess = (PlayService)sess;
                        playSess.SendTick(world);
                    }
                }
                
            }

            wssv.Stop();
        }
    }
}
