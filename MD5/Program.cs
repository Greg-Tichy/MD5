using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Crypto = System.Security.Cryptography;

namespace MD5
{
    class Program
    {
        static void Main(string[] args)
        {
            var salted = false;
            byte salt = 0;
            if (args.Length > 0)
            {
                if (Byte.TryParse(args[0], NumberStyles.HexNumber, null, out salt))
                {
                    salted = true;
                }
            }
            if (!salted)
            {
                Console.WriteLine("Need a single byte hex value as parameter");
                return;
            }
            // his sample here
            // AQJCMW0DGL,I95ORWB1A7
            //var hash = Crypto.MD5.Create();
            //byte[] from = Encoding.ASCII.GetBytes("AQJCMW0DGL");
            //Array.Resize<byte>(ref from, from.Length + 1);
            //from[^1] = salt;
            //var computed = hash.ComputeHash(from);
            //hash = Crypto.MD5.Create();
            //byte[] from1 = Encoding.ASCII.GetBytes("I95ORWB1A7");
            //Array.Resize<byte>(ref from1, from1.Length + 1);
            //from1[^1] = salt;
            //var computed1 = hash.ComputeHash(from1);

            //foreach (var b in computed)
            //{
            //    Console.Write($"{b:X2} ");
            //}
            //Console.WriteLine();
            //foreach (var b in computed1)
            //{
            //    Console.Write($"{b:X2} ");
            //}
            //Console.WriteLine();

            //return;


            var count = 0;
            var outer = 1;
            const int capacity = 1024 * 1024 * 1024;
            const int lenToTest = 5;
            var work = new Work(salt);
            foreach (var g in work.GenerateAny(1))
            {
                Console.Write($"{Encoding.UTF8.GetString(g)} ");
            }

            return;

            var dictionaries = new List<Dictionary<byte[], string>>
            {
                new Dictionary<byte[], string>(capacity: capacity, new ByteArrayComparer())
            };
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            //var destination = new byte[lenToTest];
            var collisionFound = false;
            Func<byte[], string> removeLast = (inp) => {
                var s = Encoding.ASCII.GetString(inp);
                s = s.Substring(0, s.Length - 1);
                return s;
            };
            foreach (var c in work.Generate10())
            {
                count++;
                var hash = work.Compute(c);
                Array.Resize<byte>(ref hash, lenToTest);
                //Array.Copy(hash, destination, lenToTest);
                for (var i = 0; i < outer; i++)
                {
                    if (dictionaries[i].ContainsKey(hash)) //, Encoding.ASCII.GetString(c)))
                    {
                        Console.WriteLine($"collision found: {dictionaries[i][hash]},{removeLast(c)}");
                        collisionFound = true;
                        work.Print(work.ComputeWithSalt(Encoding.ASCII.GetBytes(dictionaries[i][hash])));
                        work.Print(work.Compute(c));
                        work.Print(hash);
                        break;
                    }
                }
                if (collisionFound)
                    break;
                dictionaries[^1].Add(hash, removeLast(c));
                if (count % (1024 * 1024 * 1024) == 0)
                {
                    TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value.
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    Console.Write($"{Encoding.ASCII.GetString(c)} in {elapsedTime} ");
                    count = 0;
                    dictionaries.Add(new Dictionary<byte[], string>(capacity: capacity, new ByteArrayComparer()));
                    ++outer;
                }
            }
            Console.WriteLine();
            Console.WriteLine(outer);
            return;

            //var hash = Crypto.MD5.Create();

            //byte[] from = Encoding.ASCII.GetBytes("Hello World!");
            //var computed = hash.ComputeHash(from);

            //hash.Clear();

            //foreach (var b in computed)
            //{
            //    Console.Write($"{b:X2} ");
            //}
            //Console.WriteLine();
            //Console.WriteLine($"Length is {computed.Length} bytes; salted = {salted}");
        }
    }
    class Work
    {
        public Work(byte s)
        {
            salt = s;
            hash = Crypto.MD5.Create();
        }
        private byte salt;
        private Crypto.MD5 hash;
        public byte[] Compute(byte[] value)
        {
            return hash.ComputeHash(value);
        }
        public byte[] ComputeWithSalt(byte[] value)
        {
            Array.Resize<byte>(ref value, value.Length + 1);
            value[^1] = salt;
            return Compute(value);
        }
        public IEnumerable<byte> Values()
        {
            byte result = (byte)(Convert.ToByte('0') - 1);  // -1 because of ++result below
            while (result < Convert.ToByte('9'))
                yield return ++result;

            result = (byte)(Convert.ToByte('A') - 1);
            while (result < Convert.ToByte('Z'))
                yield return ++result;

            result = (byte)(Convert.ToByte('a') - 1);
            while (result < Convert.ToByte('z'))
                yield return ++result;
        }
        public IEnumerable<byte[]> GenerateAny(int len)
        {
            if (len == 0)
            {
                yield return new byte[0];
                yield break;
            }
            foreach (var one in GenerateAny(len - 1))
            foreach (var single in Values())
            {
                var two = new byte[one.Length + 1]; 
                Array.Copy(one, two, one.Length);
                two[^1] = single;
                    yield return two;
            }
        }
        public IEnumerable<byte[]> Generate1()
        {
            byte[] result = new byte[2];
            result[^1] = salt;
            foreach (var p in GenerateForPos(0, result))
                yield return p;
        }
        public IEnumerable<byte[]> Generate2()
        {
            byte[] result = new byte[3];
            result[^1] = salt;
            foreach (var prop in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, prop))
                    yield return p;
        }
        public IEnumerable<byte[]> Generate3()
        {
            byte[] result = new byte[4];
            result[^1] = salt;
            foreach (var prop in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, prop))
                    foreach (var p0 in GenerateForPos(2, p))
                        yield return p0;
        }
        public IEnumerable<byte[]> Generate4()
        {
            byte[] result = new byte[5];
            result[^1] = salt;
            foreach (var prop in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, prop))
                    foreach (var p1 in GenerateForPos(2, p))
                        foreach (var p2 in GenerateForPos(3, p1))
                            yield return p2;
        }
        public IEnumerable<byte[]> Generate5()
        {
            byte[] result = new byte[6];
            result[^1] = salt;
            foreach (var prop in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, prop))
                    foreach (var p1 in GenerateForPos(2, p))
                        foreach (var p2 in GenerateForPos(3, p1))
                            foreach (var p3 in GenerateForPos(4, p2))
                                yield return p3;
        }
        public IEnumerable<byte[]> Generate6()
        {
            byte[] result = new byte[7];
            result[^1] = salt;
            foreach (var o in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, o))
                    foreach (var p0 in GenerateForPos(2, p))
                        foreach (var p1 in GenerateForPos(3, p0))
                            foreach (var p2 in GenerateForPos(4, p1))
                                foreach (var p3 in GenerateForPos(5, p2))
                                    yield return p3;
        }
        public IEnumerable<byte[]> Generate7()
        {
            byte[] result = new byte[8];
            result[^1] = salt;
            foreach (var o in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, o))
                    foreach (var p0 in GenerateForPos(2, p))
                        foreach (var p1 in GenerateForPos(3, p0))
                            foreach (var p2 in GenerateForPos(4, p1))
                                foreach (var p3 in GenerateForPos(5, p2))
                                    foreach (var p4 in GenerateForPos(6, p3))
                                        yield return p4;
        }
        public IEnumerable<byte[]> Generate8()
        {
            byte[] result = new byte[9];
            result[^1] = salt;
            foreach (var o in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, o))
                    foreach (var p0 in GenerateForPos(2, p))
                        foreach (var p1 in GenerateForPos(3, p0))
                            foreach (var p2 in GenerateForPos(4, p1))
                                foreach (var p3 in GenerateForPos(5, p2))
                                    foreach (var p4 in GenerateForPos(6, p3))
                                        foreach (var p5 in GenerateForPos(7, p4))
                                            yield return p5;
        }
        public IEnumerable<byte[]> Generate9()
        {
            byte[] result = new byte[10];
            result[^1] = salt;
            foreach (var o in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, o))
                    foreach (var p0 in GenerateForPos(2, p))
                        foreach (var p1 in GenerateForPos(3, p0))
                            foreach (var p2 in GenerateForPos(4, p1))
                                foreach (var p3 in GenerateForPos(5, p2))
                                    foreach (var p4 in GenerateForPos(6, p3))
                                        foreach (var p5 in GenerateForPos(7, p4))
                                            foreach (var p6 in GenerateForPos(8, p5))
                                                yield return p6;
        }
        public IEnumerable<byte[]> Generate10()
        {
            byte[] result = new byte[11];
            result[^1] = salt;
            foreach (var o in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, o))
                    foreach (var p0 in GenerateForPos(2, p))
                        foreach (var p1 in GenerateForPos(3, p0))
                            foreach (var p2 in GenerateForPos(4, p1))
                                foreach (var p3 in GenerateForPos(5, p2))
                                    foreach (var p4 in GenerateForPos(6, p3))
                                        foreach (var p5 in GenerateForPos(7, p4))
                                            foreach (var p6 in GenerateForPos(8, p5))
                                                foreach (var p7 in GenerateForPos(9, p6))
                                                    yield return p7;
        }
        public IEnumerable<byte[]> Generate11()
        {
            byte[] result = new byte[12];
            result[^1] = salt;
            foreach (var o in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, o))
                    foreach (var p0 in GenerateForPos(2, p))
                        foreach (var p1 in GenerateForPos(3, p0))
                            foreach (var p2 in GenerateForPos(4, p1))
                                foreach (var p3 in GenerateForPos(5, p2))
                                    foreach (var p4 in GenerateForPos(6, p3))
                                        foreach (var p5 in GenerateForPos(7, p4))
                                            foreach (var p6 in GenerateForPos(8, p5))
                                                foreach (var p7 in GenerateForPos(9, p6))
                                                    foreach (var p8 in GenerateForPos(10, p7))
                                                        yield return p8;
        }
        public IEnumerable<byte[]> Generate12()
        {
            byte[] result = new byte[13];
            result[^1] = salt;
            foreach (var o in GenerateForPos(0, result))
                foreach (var p in GenerateForPos(1, o))
                    foreach (var p0 in GenerateForPos(2, p))
                        foreach (var p1 in GenerateForPos(3, p0))
                            foreach (var p2 in GenerateForPos(4, p1))
                                foreach (var p3 in GenerateForPos(5, p2))
                                    foreach (var p4 in GenerateForPos(6, p3))
                                        foreach (var p5 in GenerateForPos(7, p4))
                                            foreach (var p6 in GenerateForPos(8, p5))
                                                foreach (var p7 in GenerateForPos(9, p6))
                                                    foreach (var p8 in GenerateForPos(10, p7))
                                                        foreach (var p9 in GenerateForPos(11, p8))
                                                            yield return p9;
        }

        private IEnumerable<byte[]> GenerateForPos(int pos, byte[] partial)
        {
            foreach (var b in Values())
            {
                partial[pos] = b;
                yield return partial;
            }
            //foreach (var b in GenerateForPos(pos - 1, partial))
            //    yield return b;
        }
        public void Print(byte[] computed)
        {
            foreach (var b in computed)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
        }
    }
    class ByteArrayComparer : EqualityComparer<byte[]>
    {
        public override bool Equals([AllowNull] byte[] x, [AllowNull] byte[] y)
        {
            if (x == null && y == null)
                return true;
            else if (x == null || y == null)
                return false;
            if (x.Length != y.Length)
                return false;

            for (var i = 0; i < x.Length; i++)
                if (x[i] != y[i])
                    return false;
            return true;
        }

        public override int GetHashCode([DisallowNull] byte[] obj)
        {
            var s = Encoding.UTF8.GetString(obj);
            return s.GetHashCode();
        }
    }
}
