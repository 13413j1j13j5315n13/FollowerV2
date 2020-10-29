using System;
using System.Collections.Generic;
using System.Linq;

namespace FollowerV2
{
    internal class DelayHelper
    {
        private readonly HashSet<Delay> _delayList;

        public DelayHelper()
        {
            _delayList = new HashSet<Delay>();
        }

        public void AddToDelayManager(string name, Action callback, int millisecondsDelay)
        {
            var contains = _delayList.Select(d => d.Name == name).FirstOrDefault();
            if (contains) throw new ArgumentException($"Item with name '{name}' already exists");

            var delay = new Delay(name, callback, millisecondsDelay);
            _delayList.Add(delay);
        }

        public void CallFunction(string name)
        {
            var delay = _delayList.FirstOrDefault(d => d.Name == name);
            if (delay == null) throw new ArgumentException($"Item with name '{name}' was not found");

            var delta = GetDeltaInMilliseconds(delay.LastTimeCalled);
            if (delta > delay.MillisecondsDelay)
            {
                delay.Callback();
                delay.LastTimeCalled = DateTime.UtcNow;
            }
        }

        public static long GetDeltaInMilliseconds(DateTime lastTime)
        {
            var currentMs = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeMilliseconds();
            var lastTimeMs = ((DateTimeOffset) lastTime).ToUnixTimeMilliseconds();
            return currentMs - lastTimeMs;
        }

        private class Delay
        {
            public Delay(string name, Action callback, int millisecondsDelay)
            {
                Name = name;
                Callback = callback;
                MillisecondsDelay = millisecondsDelay;
                LastTimeCalled = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                ;
            }

            public string Name { get; }
            public DateTime LastTimeCalled { get; set; }
            public Action Callback { get; }
            public int MillisecondsDelay { get; }
        }
    }
}