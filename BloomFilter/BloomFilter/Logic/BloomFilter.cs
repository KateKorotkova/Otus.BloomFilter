using System;
using System.Collections;

namespace BloomFilter.Logic
{
    public class Filter<T>
	{
		private readonly int _hashFunctionCount;
		private readonly BitArray _hashBits;
		private readonly HashFunction _getHashSecondary;
        public delegate int HashFunction(T input);


		/// <summary>
		/// Создание Bloom filter.
		/// </summary>
		/// <param name="maxKeys">Количество элементов, которое подерживает фильтр.</param>
		public Filter(int maxKeys)
        {
			//Приемлемый уровень ложноположительного срабатывания
			var errorRate = BestErrorRate(maxKeys);

			//Количество элементов в BitArray
			var bitSize = ComputeBitSize(maxKeys, errorRate);

			//Количество хеш-функций
			var hashCount = ComputeOptimalHashFunctionsCount(maxKeys, errorRate);

			if (maxKeys < 1)
			{
				throw new ArgumentOutOfRangeException("maxKeys", maxKeys, "maxKeys must be > 0");
			}

			if (errorRate >= 1 || errorRate <= 0)
			{
				throw new ArgumentOutOfRangeException("errorRate", errorRate, string.Format("errorRate must be between 0 and 1, exclusive. Was {0}", errorRate));
			}

			if (bitSize < 1)
			{
				throw new ArgumentOutOfRangeException(string.Format("The provided maxKeys and errorRate values would result in an array of length > int.MaxValue. Please reduce either of these values. Capacity: {0}, Error rate: {1}", maxKeys, errorRate));
			}

            if (typeof(T) == typeof(string))
            {
                this._getHashSecondary = HashString;
            }
            else if (typeof(T) == typeof(int))
            {
                this._getHashSecondary = HashInt32;
            }
            else
            {
                throw new ArgumentNullException("hashFunction", "Please provide a hash function for your type T, when T is not a string or int.");
            }

			this._hashFunctionCount = hashCount;
			this._hashBits = new BitArray(bitSize);
		}

		public void Add(T item)
		{
			var hash1 = item.GetHashCode();
			var hash2 = this._getHashSecondary(item);
			
            for (var i = 0; i < this._hashFunctionCount; i++)
			{
				var hash = this.ComputeCompositeHash(hash1, hash2, i);
				this._hashBits[hash] = true;
			}
		}

		public bool Contains(T item)
		{
			int primaryHash = item.GetHashCode();
			int secondaryHash = this._getHashSecondary(item);

			for (int i = 0; i < this._hashFunctionCount; i++)
			{
				int hash = this.ComputeCompositeHash(primaryHash, secondaryHash, i);
				if (this._hashBits[hash] == false)
				{
                    //истинно-отрицательный ответ
					return false;
				}
			}

            //все хеш-функции выдали, что бит установлен. Возвращаем положительный ответ
			return true;
		}


        #region Support Methods

        private static int ComputeOptimalHashFunctionsCount(int maxKeys, float errorRate)
        {
            return (int)Math.Round(Math.Log(2.0) * ComputeBitSize(maxKeys, errorRate) / maxKeys);
        }

        /// <summary>
        /// The best M.
        /// </summary>
        private static int ComputeBitSize(int maxKeys, float errorRate)
        {
            return (int)Math.Ceiling(maxKeys * Math.Log(errorRate, (1.0 / Math.Pow(2, Math.Log(2.0)))));
        }

        /// <summary>
        /// The best error rate.
        /// </summary>
        private static float BestErrorRate(int maxKeys)
        {
            float c = (float)(1.0 / maxKeys);
            if (c != 0)
            {
                return c;
            }

            return (float)Math.Pow(0.6185, int.MaxValue / maxKeys);
        }

        /// <summary>
        /// Hashes a 32-bit signed int using Thomas Wang's method v3.1 (http://www.concentric.net/~Ttwang/tech/inthash.htm).
        /// Runtime is suggested to be 11 cycles. 
        /// </summary>
        /// <param name="input">The integer to hash.</param>
        /// <returns>The hashed result.</returns>
        private static int HashInt32(T input)
        {
            var x = input as uint?;
            return x.GetHashCode();
        }

        /// <summary>
        /// Hashes a string using Bob Jenkin's "One At A Time" method from Dr. Dobbs (http://burtleburtle.net/bob/hash/doobs.html).
        /// Runtime is suggested to be 9x+9, where x = input.Length. 
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <returns>The hashed result.</returns>
        private static int HashString(T input)
        {
            return input.ToString().GetHashCode();
        }

        /// <summary>
        /// Вычисление итоговой (композит) хеш-функции на базе двух рассчитанных
        /// </summary>
        /// <param name="primaryHash"> The primary hash. </param>
        /// <param name="secondaryHash"> The secondary hash. </param>
        /// <param name="i"> The i. </param>
        /// <returns> The <see cref="int"/>. </returns>
        private int ComputeCompositeHash(int primaryHash, int secondaryHash, int i)
        {
            int resultingHash = (primaryHash + (i * secondaryHash)) % this._hashBits.Count;
            return Math.Abs((int)resultingHash);
        }

        #endregion
	}
}