using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Curator.NET.Test
{
    /**
 * Utility to get various testing times
 */
    public class Timing
    {
        private  TimeSpan value;
        
        private  int waitingMultiple;

        private static int DEFAULT_SECONDS = 10;
        private static int DEFAULT_WAITING_MULTIPLE = 5;
        private static double SESSION_MULTIPLE = .25;

        /**
         * Use the default base time
         */
        public Timing(): this(1, getWaitingMultiple())
        {
            
        }

        /**
         * Use a multiple of the default base time
         *
         * @param multiple the multiple
         */
        public Timing(double multiple) : this(multiple, getWaitingMultiple())
        {
           
        }

        /**
         * Use a multiple of the default base time
         *
         * @param multiple the multiple
         * @param waitingMultiple multiple of main timing to use when waiting
         */
        public Timing(double multiple, int waitingMultiple)
        {
            this.value = TimeSpan.FromSeconds(DEFAULT_SECONDS * multiple);
            this.waitingMultiple = waitingMultiple;
        }

       

        /**
         * @param value base time
         * @param unit  base time unit
         */
        public Timing(TimeSpan value) : this(value, getWaitingMultiple())
        {
           
        }

        /**
         * @param value base time
         * @param unit  base time unit
         * @param waitingMultiple multiple of main timing to use when waiting
         */
        public Timing(TimeSpan value, int waitingMultiple)
        {
            this.value = value;
            this.waitingMultiple = waitingMultiple;
        }

        /**
         * Return the base time in milliseconds
         *
         * @return time ms
         */
        public int milliseconds()
        {
            return (int)value.TotalMilliseconds;
        }

        /**
         * Return the base time in seconds
         *
         * @return time secs
         */
        public int seconds()
        {
            return (int)value.TotalSeconds;
        }

        /**
         * Wait on the given latch
         *
         * @param latch latch to wait on
         * @return result of {@link CountDownLatch#await(long, TimeUnit)}
         */
		public bool awaitLatch(AutoResetEvent latch)
        {
            Timing m = forWaiting();
            try
            {
                return latch.WaitOne(value);
            }
            catch (ThreadInterruptedException e)
            {
                Thread.CurrentThread.Interrupt();
            }
            return false;
        }

        /**
         * Wait on the given semaphore
         *
         * @param semaphore the semaphore
         * @return result of {@link Semaphore#tryAcquire()}
         */
        public bool acquireSemaphore(Semaphore semaphore)
        {
            Timing m = forWaiting();
            try
            {
                
                return semaphore.WaitOne(value);
            }
            catch (ThreadInterruptedException e)
            {
                Thread.CurrentThread.Interrupt();
            }
            return false;
        }

        /**
         * Wait on the given semaphore
         *
         * @param semaphore the semaphore
         * @param n         number of permits to acquire
         * @return result of {@link Semaphore#tryAcquire(int, long, TimeUnit)}
         */
        public bool acquireSemaphore(Semaphore semaphore, int n)
        {
            Timing m = forWaiting();
            try
            {
                
                return semaphore.WaitOne();
            }
            catch (ThreadInterruptedException e)
            {
                Thread.CurrentThread.Interrupt();
            }
            return false;
        }

        /**
         * Return a new timing that is a multiple of the this timing
         *
         * @param n the multiple
         * @return this timing times the multiple
         */
        public Timing multiple(double n)
        {
            var v = new Timing(TimeSpan.FromSeconds((value.TotalSeconds * n)));
            return v;
        }

    /**
     * Return a new timing with the standard multiple for waiting on latches, etc.
     *
     * @return this timing multiplied
     */
  
        public Timing forWaiting()
        {
            return multiple(waitingMultiple);
        }

        /**
         * Sleep for a small amount of time
         *
         * @throws InterruptedException if interrupted
         */
        public void sleepABit() 
        {
            Thread.Sleep((int)value.TotalMilliseconds / 4);
           
        }

        /**
         * Return the value to use for ZK session timeout
         *
         * @return session timeout
         */
        public int session()
        {
            return multiple(SESSION_MULTIPLE).milliseconds();
        }

        /**
         * Return the value to use for ZK connection timeout
         *
         * @return connection timeout
         */
        public int connection()
        {
            return milliseconds();
        }

        private static int getWaitingMultiple()
        {
            return  DEFAULT_WAITING_MULTIPLE;
        }
    }

}
