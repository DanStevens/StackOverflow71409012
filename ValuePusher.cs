using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;

namespace StackOverflow71409012
{
    public sealed class ValuePusher : IDisposable
    {
        private readonly ValueReceiver _receiver;
        private readonly IScheduler _scheduler;
        private readonly Subject<int> _subject;

        private int _value;

        public ValuePusher(ValueReceiver receiver, IScheduler scheduler)
        {
            _receiver = receiver;
            _scheduler = scheduler;

            // Arrange to push values to `receiver` dependency
            _subject = new Subject<int>();
            _subject.ObserveOn(_scheduler)
                //.Sample(TimeSpan.FromMilliseconds(50), _scheduler)
                .SubscribeOn(_scheduler)
                .Subscribe(i => PushCurrentValueToReceiver());
        }

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                _subject.OnNext(0);
            }
        }

        private void PushCurrentValueToReceiver()
        {
            _receiver.Value = Value;
        }

        public void Dispose()
        {
            _subject?.OnCompleted();
            _subject?.Dispose();
        }
    }

    public class ValueReceiver
    {
        public int Value { get; set; }
    }

    [TestClass]
    public class ValuePusherTests
    {
        [TestMethod]
        [Timeout(1000)]
        public void ReceiverReceivesValueFromPusherViaScheduler()
        {
            var scheduler = new TestScheduler();
            var receiver = new ValueReceiver();

            using (var pusher = new ValuePusher(receiver, scheduler))
            {
                scheduler.Start();
                pusher.Value = 1;
                scheduler.AdvanceBy(1);
                Assert.AreEqual(1, receiver.Value);
            }
        }
    }
}
