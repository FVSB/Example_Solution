using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace PowerPositionCalculator
{
    /// <summary>
    /// Represents a thread-safe asynchronous array with per-index locking for concurrent access.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    public class AsyncTradesVolumeArray<T> : IAsyncEnumerable<T>
    {
        protected readonly T[] Array;
        protected readonly SemaphoreSlim[] Locks;

        /// <summary>
        /// Gets the length of the array.
        /// </summary>
        public int Length => Array.Length;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTradesVolumeArray{T}"/> class with the specified length.
        /// </summary>
        /// <param name="length">The size of the array.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="length"/> is less than or equal to zero.</exception>
        public AsyncTradesVolumeArray(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

            Array = new T[length];
            Locks = new SemaphoreSlim[length];

            for (int i = 0; i < length; i++)
                Locks[i] = new SemaphoreSlim(1, 1);

            Log.Information("Initialized AsyncTradesVolumeArray with length {Length}.", length);
        }

        /// <summary>
        /// Provides thread-safe synchronous access to array elements.
        /// </summary>
        /// <param name="index">Index of the element.</param>
        public T this[int index]
        {
            get
            {
                CheckIndex(index);
                Log.Debug("Synchronous get on index {Index}.", index);
                Locks[index].Wait();
                try
                {
                    return Array[index];
                }
                finally
                {
                    Locks[index].Release();
                    Log.Debug("Released lock after synchronous get on index {Index}.", index);
                }
            }
            set
            {
                CheckIndex(index);
                Log.Debug("Synchronous set on index {Index} with value {Value}.", index, value);
                Locks[index].Wait();
                try
                {
                    Array[index] = value;
                }
                finally
                {
                    Locks[index].Release();
                    Log.Debug("Released lock after synchronous set on index {Index}.", index);
                }
            }
        }

        /// <summary>
        /// Asynchronously retrieves the element at the specified index.
        /// </summary>
        /// <param name="index">Index of the element.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The value at the specified index.</returns>
        public async Task<T> GetAsync(int index, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            CheckIndex(index);
            Log.Debug("Async get on index {Index}.", index);
            await Locks[index].WaitAsync(ct);
            try
            {
                return Array[index];
            }
            finally
            {
                Locks[index].Release();
                Log.Debug("Released lock after async get on index {Index}.", index);
            }
        }

        /// <summary>
        /// Asynchronously sets the element at the specified index.
        /// </summary>
        /// <param name="index">Index of the element.</param>
        /// <param name="value">Value to set.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task SetAsync(int index, T value, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            CheckIndex(index);
            Log.Debug("Async set on index {Index} with value {Value}.", index, value);
            await Locks[index].WaitAsync(ct);
            try
            {
                Array[index] = value;
            }
            finally
            {
                Locks[index].Release();
                Log.Debug("Released lock after async set on index {Index}.", index);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct)
        {
            for (int i = 0; i < Array.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                Log.Debug("Async enumerator accessing index {Index}.", i);

                await Locks[i].WaitAsync(ct);
                try
                {
                    yield return Array[i];
                }
                finally
                {
                    Locks[i].Release();
                    Log.Debug("Released lock after enumerator access on index {Index}.", i);
                }
            }
        }

        /// <summary>
        /// Validates the specified index to ensure it is within the bounds of the array.
        /// </summary>
        /// <param name="index">Index to validate.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
        protected void CheckIndex(int index)
        {
            if (index < 0 || index >= Array.Length)
            {
                Log.Error("Index {Index} is out of range for array length {Length}.", index, Array.Length);
                throw new IndexOutOfRangeException($"Index {index} is out of range.");
            }
        }
    }

    /// <summary>
    /// Represents a thread-safe asynchronous array specialized for double values,
    /// supporting additional operations for concurrent numeric calculations.
    /// </summary>
    public class AsyncTradesVolumeTradesVolumenCalculator : AsyncTradesVolumeArray<double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTradesVolumeTradesVolumenCalculator"/> class.
        /// </summary>
        /// <param name="length">The size of the array.</param>
        public AsyncTradesVolumeTradesVolumenCalculator(int length) : base(length)
        {
            Log.Debug("Initialized AsyncTradesVolumeTradesVolumenCalculator with length {Length}.", length);
        }

        /// <summary>
        /// Asynchronously adds a value to the element at the specified index.
        /// </summary>
        /// <param name="index">Index of the element.</param>
        /// <param name="value">Value to add.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task AddAsync(int index, double value, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            CheckIndex(index);
            Log.Debug("Async add on index {Index} with increment {Value}.", index, value);
            await Locks[index].WaitAsync(ct);
            try
            {
                Array[index] += value;
            }
            finally
            {
                Locks[index].Release();
                Log.Debug("Released lock after async add on index {Index}.", index);
            }
        }

        /// <summary>
        /// Asynchronously adds a value to all elements in the array.
        /// </summary>
        /// <param name="value">Value to add to each element.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task AddToAllAsync(double value, CancellationToken ct)
        {
            Log.Debug("Async add to all elements with increment {Value}.", value);
            for (int i = 0; i < Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Locks[i].WaitAsync(ct);
                try
                {
                    Array[i] += value;
                }
                finally
                {
                    Locks[i].Release();
                    Log.Debug("Released lock after async add on index {Index}.", i);
                }
            }
        }

        /// <summary>
        /// Retrieves a shallow copy of the underlying array.
        /// </summary>
        /// <returns>A clone of the array.</returns>
        public double[] GetArray()
        {
            lock (Array)
            {
                Log.Debug("Cloning the internal array.");
                return (double[])Array.Clone();
            }
        }
    }
}
