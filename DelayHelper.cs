using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerV2
{
    class DelayHelper
    {
        private HashSet<Delay> _delayList;

        public DelayHelper()
        {
            _delayList = new HashSet<Delay>();
        }

        public void AddToDelayManager(string name, Action callback, int millisecondsDelay)
        {
            bool contains = _delayList.Select(d => d.Name == name).FirstOrDefault();
            if (contains) throw new ArgumentException($"Item with name '{name}' already exists");

            Delay delay = new Delay(name, callback, millisecondsDelay);
            _delayList.Add(delay);
        }

        public void CallFunction(string name)
        {
            Delay delay = _delayList.FirstOrDefault(d => d.Name == name);
            if (delay == null) throw new ArgumentException($"Item with name '{name}' was not found");

            long delta = GetDeltaInMilliseconds(delay.LastTimeCalled);
            if (delta > delay.MillisecondsDelay)
            {
                delay.Callback();
                delay.LastTimeCalled = DateTime.UtcNow;
            }
        }

        public static long GetDeltaInMilliseconds(DateTime lastTime)
        {
            long currentMs = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            long lastTimeMs = ((DateTimeOffset)lastTime).ToUnixTimeMilliseconds();
            return currentMs - lastTimeMs;
        }

        private class Delay
        {
            public string Name { get; }
            public DateTime LastTimeCalled { get; set; }
            public Action Callback { get; }
            public int MillisecondsDelay { get; }

            public Delay(string name, Action callback, int millisecondsDelay)
            {
                Name = name;
                Callback = callback;
                MillisecondsDelay= millisecondsDelay;
                LastTimeCalled = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc); ;
            }
        }
    }
}
