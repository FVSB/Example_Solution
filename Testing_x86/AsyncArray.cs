namespace PowerPositionCalculator;

    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncArray<T> : IAsyncEnumerable<T>
    {
        protected readonly T[] _array;
        protected readonly SemaphoreSlim[] _locks;

        public int Length => _array.Length;

        public AsyncArray(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "La longitud debe ser mayor que cero.");

            _array = new T[length];
            _locks = new SemaphoreSlim[length];

            for (int i = 0; i < length; i++)
                _locks[i] = new SemaphoreSlim(1, 1);
        }

        // ✅ Indexador para acceso sincronizado
        public T this[int index]
        {
            get
            {
                CheckIndex(index);
                _locks[index].Wait();
                try
                {
                    return _array[index];
                }
                finally
                {
                    _locks[index].Release();
                }
            }
            set
            {
                CheckIndex(index);
                _locks[index].Wait();
                try
                {
                    _array[index] = value;
                }
                finally
                {
                    _locks[index].Release();
                }
            }
        }

        // ✅ Métodos asíncronos para lectura/escritura
        public async Task<T> GetAsync(int index ,CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            CheckIndex(index);
            await _locks[index].WaitAsync(ct);
            try
            {
                return _array[index];
            }
            finally
            {
                _locks[index].Release();
            }
        }

        public async Task SetAsync(int index, T value, CancellationToken ct)
        {
            CheckIndex(index);
            await _locks[index].WaitAsync(ct);
            try
            {
                _array[index] = value;
            }
            finally
            {
                _locks[index].Release();
            }
        }


        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct)
        {
            for (int i = 0; i < _array.Length; i++)
            {
                ct.ThrowIfCancellationRequested();

                await _locks[i].WaitAsync(ct);
                try
                {
                    yield return _array[i];
                }
                finally
                {
                    _locks[i].Release();
                }
            }
        }

        protected void CheckIndex(int index)
        {
            if (index < 0 || index >= _array.Length)
                throw new IndexOutOfRangeException($"El índice {index} está fuera de rango.");
        }
    }

public class AsyncTradesVolumenCalculator : AsyncArray<double>
{
    public AsyncTradesVolumenCalculator(int length) : base(length)
    {
    }

    /// <summary>
    /// Suma un valor al contenido actual en el índice especificado de forma segura entre hilos.
    /// </summary>
    /// <param name="index">Índice del array.</param>
    /// <param name="value">Valor a sumar.</param>
    public async Task AddAsync(int index, double value, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Validamos índice
        CheckIndex(index);

        // Bloqueamos el semáforo para este índice
        await _locks[index].WaitAsync(ct);
        try
        {
            _array[index] += value;
        }
        finally
        {
            _locks[index].Release();
        }
    }

    /// <summary>
    /// Suma un valor a todos los elementos del array de forma segura entre hilos.
    /// </summary>
    /// <param name="value">Valor a sumar.</param>
    public async Task AddToAllAsync(double value,CancellationToken ct)
    {
        for (int i = 0; i < Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            await _locks[i].WaitAsync(ct);
            try
            {
                _array[i] += value;
            }
            finally
            {
                _locks[i].Release();
            }
        }
    }

    public double[] GetArray()
    {
        lock (this._array)
        {
            return (double[])this._array.Clone();
        }
    }

}