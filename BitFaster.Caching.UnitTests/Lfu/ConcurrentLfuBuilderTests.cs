﻿using System;
using BitFaster.Caching.Atomic;
using BitFaster.Caching.Lfu;
using BitFaster.Caching.Scheduler;
using FluentAssertions;
using Xunit;

namespace BitFaster.Caching.UnitTests.Lfu
{
    public class ConcurrentLfuBuilderTests
    {
        [Fact]
        public void TestConcurrencyLevel()
        {
            var b = new ConcurrentLfuBuilder<int, int>()
                .WithConcurrencyLevel(0);

            Action constructor = () => { var x = b.Build(); };

            constructor.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TestIntCapacity()
        {
            ICache<int, int> lfu = new ConcurrentLfuBuilder<int, int>()
                .WithCapacity(3)
                .Build();

            lfu.Policy.Eviction.Value.Capacity.Should().Be(3);
        }

        [Fact]
        public void TestScheduler()
        {
            ICache<int, int> lfu = new ConcurrentLfuBuilder<int, int>()
                .WithScheduler(new NullScheduler())
                .Build();

            var clfu = lfu as ConcurrentLfu<int, int>;
            clfu.Scheduler.Should().BeOfType<NullScheduler>();
        }

        [Fact]
        public void TestComparer()
        {
            ICache<string, int> lfu = new ConcurrentLfuBuilder<string, int>()
                .WithKeyComparer(StringComparer.OrdinalIgnoreCase)
                .Build();

            lfu.GetOrAdd("a", k => 1);
            lfu.TryGet("A", out var value).Should().BeTrue();
        }

        [Fact]
        public void TestExpireAfterAccess()
        {
            ICache<string, int> expireAfterAccess = new ConcurrentLfuBuilder<string, int>()
                .WithExpireAfterAccess(TimeSpan.FromSeconds(1))
                .Build();

            expireAfterAccess.Policy.ExpireAfterAccess.HasValue.Should().BeTrue();
            expireAfterAccess.Policy.ExpireAfterAccess.Value.TimeToLive.Should().Be(TimeSpan.FromSeconds(1));
            expireAfterAccess.Policy.ExpireAfterWrite.HasValue.Should().BeFalse();
        }

        [Fact]
        public void TestExpireAfterReadAndExpireAfterWriteThrows()
        {
            var builder = new ConcurrentLfuBuilder<string, int>()
                .WithExpireAfterAccess(TimeSpan.FromSeconds(1))
                .WithExpireAfterWrite(TimeSpan.FromSeconds(2));

            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TestExpireAfter()
        {
            ICache<string, int> expireAfter = new ConcurrentLfuBuilder<string, int>()
                .WithExpireAfter(new TestExpiryCalculator<string, int>((k, v) => Duration.FromMinutes(5)))
                .Build();

            expireAfter.Policy.ExpireAfter.HasValue.Should().BeTrue();

            expireAfter.Policy.ExpireAfterAccess.HasValue.Should().BeFalse();
            expireAfter.Policy.ExpireAfterWrite.HasValue.Should().BeFalse();
        }

        [Fact]
        public void TestAsyncExpireAfter()
        {
            IAsyncCache<string, int> expireAfter = new ConcurrentLfuBuilder<string, int>()
                .AsAsyncCache()
                .WithExpireAfter(new TestExpiryCalculator<string, int>((k, v) => Duration.FromMinutes(5)))
                .Build();

            expireAfter.Policy.ExpireAfter.HasValue.Should().BeTrue();

            expireAfter.Policy.ExpireAfterAccess.HasValue.Should().BeFalse();
            expireAfter.Policy.ExpireAfterWrite.HasValue.Should().BeFalse();
        }


        [Fact]
        public void TestExpireAfterWriteAndExpireAfterThrows()
        {
            var builder = new ConcurrentLfuBuilder<string, int>()
                .WithExpireAfterWrite(TimeSpan.FromSeconds(1))
                .WithExpireAfter(new TestExpiryCalculator<string, int>((k, v) => Duration.FromMinutes(5)));

            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TestExpireAfterAccessAndExpireAfterThrows()
        {
            var builder = new ConcurrentLfuBuilder<string, int>()
                .WithExpireAfterAccess(TimeSpan.FromSeconds(1))
                .WithExpireAfter(new TestExpiryCalculator<string, int>((k, v) => Duration.FromMinutes(5)));

            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TestExpireAfterAccessAndWriteAndExpireAfterThrows()
        {
            var builder = new ConcurrentLfuBuilder<string, int>()
                .WithExpireAfterWrite(TimeSpan.FromSeconds(1))
                .WithExpireAfterAccess(TimeSpan.FromSeconds(1))
                .WithExpireAfter(new TestExpiryCalculator<string, int>((k, v) => Duration.FromMinutes(5)));

            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TestScopedWithExpireAfterThrows()
        {
            var builder = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfter(new TestExpiryCalculator<string, Disposable>((k, v) => Duration.FromMinutes(5)))
                .AsScopedCache();

            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TestScopedAtomicWithExpireAfterThrows()
        {
            var builder = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfter(new TestExpiryCalculator<string, Disposable>((k, v) => Duration.FromMinutes(5)))
                .AsScopedCache()
                .WithAtomicGetOrAdd();

            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TestAsyncScopedWithExpireAfterThrows()
        {
            var builder = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfter(new TestExpiryCalculator<string, Disposable>((k, v) => Duration.FromMinutes(5)))
                .AsAsyncCache()
                .AsScopedCache();

            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TestAsyncScopedAtomicWithExpireAfterThrows()
        {
            var builder = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfter(new TestExpiryCalculator<string, Disposable>((k, v) => Duration.FromMinutes(5)))
                .AsAsyncCache()
                .AsScopedCache()
                .WithAtomicGetOrAdd();

            Action act = () => builder.Build();
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TestScopedWithExpireAfterWrite()
        {
            var expireAfterWrite = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfterWrite(TimeSpan.FromSeconds(1))
                .AsScopedCache()
                .Build();

            expireAfterWrite.Policy.ExpireAfterWrite.HasValue.Should().BeTrue();
            expireAfterWrite.Policy.ExpireAfterWrite.Value.TimeToLive.Should().Be(TimeSpan.FromSeconds(1));
            expireAfterWrite.Policy.ExpireAfterAccess.HasValue.Should().BeFalse();
        }

        [Fact]
        public void TestScopedWithExpireAfterAccess()
        {
            var expireAfterAccess = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfterAccess(TimeSpan.FromSeconds(1))
                .AsScopedCache()
                .Build();

            expireAfterAccess.Policy.ExpireAfterAccess.HasValue.Should().BeTrue();
            expireAfterAccess.Policy.ExpireAfterAccess.Value.TimeToLive.Should().Be(TimeSpan.FromSeconds(1));
            expireAfterAccess.Policy.ExpireAfterWrite.HasValue.Should().BeFalse();
        }

        [Fact]
        public void TestAtomicWithExpireAfterWrite()
        {
            var expireAfterWrite = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfterWrite(TimeSpan.FromSeconds(1))
                .WithAtomicGetOrAdd()
                .Build();

            expireAfterWrite.Policy.ExpireAfterWrite.HasValue.Should().BeTrue();
            expireAfterWrite.Policy.ExpireAfterWrite.Value.TimeToLive.Should().Be(TimeSpan.FromSeconds(1));
            expireAfterWrite.Policy.ExpireAfterAccess.HasValue.Should().BeFalse();
        }

        [Fact]
        public void TestAtomicWithExpireAfterAccess()
        {
            var expireAfterAccess = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfterAccess(TimeSpan.FromSeconds(1))
                .WithAtomicGetOrAdd()
                .Build();

            expireAfterAccess.Policy.ExpireAfterAccess.HasValue.Should().BeTrue();
            expireAfterAccess.Policy.ExpireAfterAccess.Value.TimeToLive.Should().Be(TimeSpan.FromSeconds(1));
            expireAfterAccess.Policy.ExpireAfterWrite.HasValue.Should().BeFalse();
        }

        [Fact]
        public void TestScopedAtomicWithExpireAfterWrite()
        {
            var expireAfterWrite = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfterWrite(TimeSpan.FromSeconds(1))
                .AsScopedCache()
                .WithAtomicGetOrAdd()
                .Build();

            expireAfterWrite.Policy.ExpireAfterWrite.HasValue.Should().BeTrue();
            expireAfterWrite.Policy.ExpireAfterWrite.Value.TimeToLive.Should().Be(TimeSpan.FromSeconds(1));
            expireAfterWrite.Policy.ExpireAfterAccess.HasValue.Should().BeFalse();
        }

        [Fact]
        public void TestScopedAtomicWithExpireAfterAccess()
        {
            var expireAfterAccess = new ConcurrentLfuBuilder<string, Disposable>()
                .WithExpireAfterAccess(TimeSpan.FromSeconds(1))
                .AsScopedCache()
                .WithAtomicGetOrAdd()
                .Build();

            expireAfterAccess.Policy.ExpireAfterAccess.HasValue.Should().BeTrue();
            expireAfterAccess.Policy.ExpireAfterAccess.Value.TimeToLive.Should().Be(TimeSpan.FromSeconds(1));
            expireAfterAccess.Policy.ExpireAfterWrite.HasValue.Should().BeFalse();
        }

        // 1
        [Fact]
        public void WithScopedValues()
        {
            IScopedCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .AsScopedCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeOfType<ScopedCache<int, Disposable>>();
            lru.Policy.Eviction.Value.Capacity.Should().Be(3);
        }

        // 2
        [Fact]
        public void WithAtomicFactory()
        {
            ICache<int, int> lru = new ConcurrentLfuBuilder<int, int>()
                .WithAtomicGetOrAdd()
                .WithCapacity(3)
                .Build();

            lru.Should().BeOfType<AtomicFactoryCache<int, int>>();
        }

        // 3
        [Fact]
        public void AsAsync()
        {
            IAsyncCache<int, int> lru = new ConcurrentLfuBuilder<int, int>()
                .AsAsyncCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IAsyncCache<int, int>>();
        }

        // 4
        [Fact]
        public void WithAtomicWithScope()
        {
            IScopedCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .WithAtomicGetOrAdd()
                .AsScopedCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeOfType<AtomicFactoryScopedCache<int, Disposable>>();
            lru.Policy.Eviction.Value.Capacity.Should().Be(3);
        }

        // 5
        [Fact]
        public void WithScopedWithAtomic()
        {
            IScopedCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .AsScopedCache()
                .WithAtomicGetOrAdd()
                .WithCapacity(3)
                .Build();

            lru.Should().BeOfType<AtomicFactoryScopedCache<int, Disposable>>();
            lru.Policy.Eviction.Value.Capacity.Should().Be(3);
        }

        // 6
        [Fact]
        public void AsAsyncWithScoped()
        {
            IScopedAsyncCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .AsAsyncCache()
                .AsScopedCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IScopedAsyncCache<int, Disposable>>();

            lru.Policy.Eviction.Value.Capacity.Should().Be(3);
        }

        // 7
        [Fact]
        public void WithScopedAsAsync()
        {
            IScopedAsyncCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .AsScopedCache()
                .AsAsyncCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IScopedAsyncCache<int, Disposable>>();
            lru.Policy.Eviction.Value.Capacity.Should().Be(3);
        }

        // 8
        [Fact]
        public void WithAtomicAsAsync()
        {
            IAsyncCache<int, int> lru = new ConcurrentLfuBuilder<int, int>()
                .WithAtomicGetOrAdd()
                .AsAsyncCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IAsyncCache<int, int>>();
        }

        // 9
        [Fact]
        public void AsAsyncWithAtomic()
        {
            IAsyncCache<int, int> lru = new ConcurrentLfuBuilder<int, int>()
                .AsAsyncCache()
                .WithAtomicGetOrAdd()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IAsyncCache<int, int>>();
        }

        // 10
        [Fact]
        public void WithAtomicWithScopedAsAsync()
        {
            IScopedAsyncCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .WithAtomicGetOrAdd()
                .AsScopedCache()
                .AsAsyncCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IScopedAsyncCache<int, Disposable>>();
        }

        // 11
        [Fact]
        public void WithAtomicAsAsyncWithScoped()
        {
            IScopedAsyncCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .WithAtomicGetOrAdd()
                .AsAsyncCache()
                .AsScopedCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IScopedAsyncCache<int, Disposable>>();
        }

        // 12
        [Fact]
        public void WithScopedWithAtomicAsAsync()
        {
            IScopedAsyncCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .AsScopedCache()
                .WithAtomicGetOrAdd()
                .AsAsyncCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IScopedAsyncCache<int, Disposable>>();
        }

        // 13
        [Fact]
        public void WithScopedAsAsyncWithAtomic()
        {
            IScopedAsyncCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .AsScopedCache()
                .AsAsyncCache()
                .WithAtomicGetOrAdd()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IScopedAsyncCache<int, Disposable>>();
        }

        // 14
        [Fact]
        public void AsAsyncWithScopedWithAtomic()
        {
            IScopedAsyncCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .AsAsyncCache()
                .AsScopedCache()
                .WithAtomicGetOrAdd()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IScopedAsyncCache<int, Disposable>>();
        }

        // 15
        [Fact]
        public void AsAsyncWithAtomicWithScoped()
        {
            IScopedAsyncCache<int, Disposable> lru = new ConcurrentLfuBuilder<int, Disposable>()
                .AsAsyncCache()
                .WithAtomicGetOrAdd()
                .AsScopedCache()
                .WithCapacity(3)
                .Build();

            lru.Should().BeAssignableTo<IScopedAsyncCache<int, Disposable>>();
        }
    }
}
