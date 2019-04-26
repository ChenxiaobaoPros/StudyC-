using System.Threading;
using System.Threading.Tasks;

namespace System.ReflectionDetail
{
    delegate string TestDelegate(string value);
    public class TestClass
    {
        public TestClass()
        {
        }
        public string GetValue(string value)
        {
            //Console.WriteLine(value);
            return value;
        }
    }
    public class Program
    {
        static void Main(string[] args)
        {
            //test1();
            //test9();
            test19();


            Console.ReadKey();
        }
        //反射
        static void test1()
        {
            //动态创建委托
            TestClass obj = new TestClass();
            //获取类型，实际上这里也可以直接用typeof来获取类型
            Type t2 = Type.GetType("System.ReflectionDetail.TestDelegate");
            //Type t3 = typeof(TestClass);
            //创建代理，传入类型、创建代理的对象以及方法名称
            TestDelegate method = (TestDelegate)Delegate.CreateDelegate(t2, obj, "GetValue");
            string returnValue = method("hello");

            Console.WriteLine(returnValue);
        }
        //Task
        static void test2()
        {
            Action<object> action = (object obj) =>
            {
                Console.WriteLine("Task={0}, obj={1}, Thread={2}",
                Task.CurrentId, obj,
                Thread.CurrentThread.ManagedThreadId);
            };

            // Create a task but do not start it.
            Task t1 = new Task(action, "alpha");

            // Construct a started task
            Task t2 = Task.Factory.StartNew(action, "beta");
            // Block the main thread to demonstrate that t2 is executing
            t2.Wait();

            // Launch t1 
            t1.Start();
            Console.WriteLine("t1 has been launched. (Main Thread={0})",
                              Thread.CurrentThread.ManagedThreadId);
            // Wait for the task to finish.
            t1.Wait();

            // Construct a started task using Task.Run.
            String taskData = "delta";
            Task t3 = Task.Run(() =>
            {
                Console.WriteLine("Task={0}, obj={1}, Thread={2}",
                                  Task.CurrentId, taskData,
                                   Thread.CurrentThread.ManagedThreadId);
            });
            // Wait for the task to finish.
            t3.Wait();

            // Construct an unstarted task
            Task t4 = new Task(action, "gamma");
            // Run it synchronously
            t4.RunSynchronously();
            // Although the task was run synchronously, it is a good practice
            // to wait for it in the event exceptions were thrown by the task.
            t4.Wait();
        }
        //TaskLoop
        static async void test3()
        {
            await Task.Run(() =>
            {
                // Just loop.
                int ctr = 0;
                for (ctr = 0; ctr <= 10; ctr++)
                {
                    Console.WriteLine(ctr + "次工作");
                }
                Console.WriteLine("Finished {0} loop iterations",
                                  ctr);
            });
        }

        #region 无返回值的方式1
        static void test4()
        {
            var t1 = new Task(() => TaskMethod1("Task 1"));
            var t2 = new Task(() => TaskMethod1("Task 2"));
            t2.Start();
            t1.Start();
            Task.WaitAll(t1, t2);
            Task.Run(() => TaskMethod1("Task 3"));
            Task.Factory.StartNew(() => TaskMethod1("Task 4"));
            //标记为长时间运行任务,则任务不会使用线程池,而在单独的线程中运行。
            Task.Factory.StartNew(() => TaskMethod1("Task 5"), TaskCreationOptions.LongRunning);

            #region 常规的使用方式
            Console.WriteLine("主线程执行业务处理.");
            //创建任务
            Task task = new Task(() =>
            {
                Console.WriteLine("使用System.Threading.Tasks.Task执行异步操作.");
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine(i);
                }
            });
            //启动任务,并安排到当前任务队列线程中执行任务(System.Threading.Tasks.TaskScheduler)
            task.Start();
            Console.WriteLine("主线程执行其他处理");
            task.Wait();
            #endregion

            Thread.Sleep(TimeSpan.FromSeconds(1));
            Console.ReadLine();

        }
        static void TaskMethod1(string name)
        {
            Console.WriteLine("Task {0} is running on a thread id {1}. Is thread pool thread: {2}",
                name, Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsThreadPoolThread);
        }
        #endregion

        #region async/await的实现方式1
        static void test5()
        {
            Console.WriteLine("主线程执行业务处理.");
            AsyncFunction();
            Console.WriteLine("主线程执行其他处理");
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(string.Format("Main:i={0}", i));
            }
            //Console.ReadLine();
        }
        //与主线程同时进行
        async static void AsyncFunction()
        {
            await Task.Delay(1);
            Console.WriteLine("使用System.Threading.Tasks.Task执行异步操作.");
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(string.Format("AsyncFunction:i={0}", i));
            }
        }
        #endregion

        #region 带返回值的方式2
        static Task<int> CreateTask(string name)
        {
            return new Task<int>(() => TaskMethod2(name));
        }
        static int TaskMethod2(string name)
        {
            Console.WriteLine("Task {0} is running on a thread id {1}. Is thread pool thread: {2}",
                name, Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsThreadPoolThread);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            return 42;
        }

        static int Getsum()
        {
            int sum = 0;
            Console.WriteLine("使用Task执行异步操作.");
            for (int i = 0; i < 10; i++)
            {
                sum += i;
            }
            return sum;
        }

        static void test6()
        {
            TaskMethod2("Main Thread Task");
            Task<int> task = CreateTask("Task 1");
            task.Start();
            int result = task.Result;
            Console.WriteLine("Task 1 Result is: {0}", result);

            task = CreateTask("Task 2");
            //该任务会运行在主线程中
            task.RunSynchronously();
            result = task.Result;
            Console.WriteLine("Task 2 Result is: {0}", result);

            task = CreateTask("Task 3");
            Console.WriteLine(task.Status);
            task.Start();

            while (!task.IsCompleted)
            {
                Console.WriteLine(task.Status);
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
            }

            Console.WriteLine(task.Status);
            result = task.Result;
            Console.WriteLine("Task 3 Result is: {0}", result);

            #region 常规使用方式
            //创建任务
            Task<int> getsumtask = new Task<int>(() => Getsum());
            //启动任务,并安排到当前任务队列线程中执行任务(System.Threading.Tasks.TaskScheduler)
            getsumtask.Start();
            Console.WriteLine("主线程执行其他处理");
            //等待任务的完成执行过程。
            getsumtask.Wait();
            //获得任务的执行结果
            Console.WriteLine("任务执行结果：{0}", getsumtask.Result.ToString());
            #endregion
        }
        #endregion

        #region async/await的实现方式2
        async static Task<int> AsyncGetsum()
        {
            await Task.Delay(1);
            int sum = 0;
            Console.WriteLine("使用Task执行异步操作.");
            for (int i = 0; i < 10; i++)
            {
                sum += i;
            }
            return sum;
        }
        static void test7()
        {
            var ret1 = AsyncGetsum();
            Console.WriteLine("主线程执行其他处理");
            for (int i = 1; i <= 3; i++)
                Console.WriteLine("Call Main()");
            int result = ret1.Result;                  //阻塞主线程
            Console.WriteLine("任务执行结果：{0}", result);
        }
        #endregion

        #region 组合任务.ContinueWith
        static void test8()
        {
            //创建一个任务
            Task<int> task = new Task<int>(() =>
            {
                int sum = 0;
                Console.WriteLine("使用Task执行异步操作.");
                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(50);
                    sum += i;
                }
                return sum;
            });
            //启动任务,并安排到当前任务队列线程中执行任务(System.Threading.Tasks.TaskScheduler)
            task.Start();
            Console.WriteLine("主线程执行其他处理");
            //任务完成时执行处理。
            Task cwt = task.ContinueWith(t =>
            {
                Console.WriteLine("任务完成后的执行结果：{0}", t.Result.ToString());
            });
            task.Wait();
            cwt.Wait();
        }
        #endregion

        #region 委托
        //简单委托
        static bool isdelegate;
        static void Book()
        {
            while (isdelegate)
            {
                Console.WriteLine("我是提供书籍的");
                Thread.Sleep(100);
            }
        }
        static void test9()
        {
            //建立联系
            BuyBook buybook = new BuyBook(Book);
            //触发
            isdelegate = true;
            buybook();
        }


        #endregion

        #region Action 简化委托 有参数并且不返回值。
        //普通Action
        static void test10()
        {
            Action BookAction = new Action(Book);
            BookAction();
        }
        //泛型委托(一个参数)
        static void test11()
        {
            Action<string> BookAction = new Action<string>(Book1);
            BookAction("百年孤独");
        }
        static void Book1(string BookName)
        {
            Console.WriteLine("我是买书的是:{0}", BookName);
        }
        //泛型委托(两个参数)
        static void test12()
        {
            Action<string, string> BookAction = new Action<string, string>(Book2);
            BookAction("百年孤独", "北京大书店");
        }
        static void Book2(string BookName, string ChangJia)
        {
            Console.WriteLine("我是买书的是:{0}来自{1}", BookName, ChangJia);
        }
        #endregion

        #region 简化委托 Func 封装一个不具有参数但却返回 TResult 参数指定的类型值的方法。
        //没有参数只有返回值的Func
        static void test13()
        {
            Func<string> RetBook = new Func<string>(FuncBook1);
            Console.WriteLine(RetBook());
        }
        static string FuncBook1()
        {
            return "送书来了";
        }
        //有参数有返回值
        static void test14()
        {
            Func<string, string> RetBook = new Func<string, string>(FuncBook2);
            Console.WriteLine(RetBook("aaa"));
        }
        static string FuncBook2(string BookName)
        {
            return BookName;
        }
        //只是传递值的Func DisplayVaue是处理传来的值，比喻缓存的处理，或者统一添加数据库等
        static void test15()
        {
            Func<string> funcValue = delegate
            {
                return "我是即将传递的值3";
            };
            DisPlayValue(funcValue);
        }
        static void DisPlayValue(Func<string> func)
        {
            string RetFunc = func();
            Console.WriteLine("我在测试一下传过来值：{0}", RetFunc);
        }
        #endregion

        #region IAsyncResult Interface表示异步操作的状态。
        //例子1
        static void test16()
        {
            int threadId;

            // Create an instance of the test class.
            AsyncTest ad = new AsyncTest();

            // Create the delegate.
            AsyncMethodCaller caller = new AsyncMethodCaller(ad.TestMethod);

            // Initiate the asychronous call.
            IAsyncResult result = caller.BeginInvoke(3000,
                out threadId, null, null);

            Thread.Sleep(1000);
            Console.WriteLine("Main thread {0} does some work.",
                Thread.CurrentThread.ManagedThreadId);

            // Wait for the WaitHandle to become signaled.
            result.AsyncWaitHandle.WaitOne();

            // Perform additional processing here.
            // Call EndInvoke to retrieve the results.
            string returnValue = caller.EndInvoke(out threadId, result);

            // Close the wait handle.
            result.AsyncWaitHandle.Close();

            Console.WriteLine("The call executed on thread {0}, with return value \"{1}\".",
                threadId, returnValue);
        }

        #endregion

        #region 委托详解

        /* 声明
         *  public delegate T1 myMethodDelegate( Type1 T2,Type1T3,... );
         *  返回类型以及参数
         * 初始化
         * 初始化一个委托，该委托对指定的类实例调用指定的实例方法。
         * protected Delegate (object target, string method);
         * 参数
         * target Object
         * 类实例，委托对其调用 method。
         * method String
         * 委托表示的实例方法的名称。
         * 
         * 
         * */

        //简单委托
        static void test17()
        {
            //委托类型 名字=new 委托类型(方法事件)
            /*  调用方法  */
            ProcessDelegate1 pd = new ProcessDelegate1(new Test().Process1);
            Console.WriteLine(pd("Text1", "Text2"));
        }
        //泛型委托
        static void test18()
        {
            /*  调用方法  */
            ProcessDelegateT<string, int> pd = new ProcessDelegateT<string, int>(new Test().Process2);
            Console.WriteLine(pd("Text1", 100));
        }

        //委托+事件
        static void test19()
        {
            /* 第一步执行 创建委托人 */
            Test1 t = new Test1();
            /* 关联事件方法，相当于寻找到了委托人 */
            t.ProcessEvent += new ProcessDelegate2(t_ProcessEvent);
            /* 进入Process方法 */
            Console.WriteLine(t.Process());
        }
        //委托事件
        static void t_ProcessEvent(object sender, EventArgs e)
        {
            Test1 t = (Test1)sender;
            t.Text1 = "Hello";
            t.Text2 = "World";
        }
        //回调函数
        static void test20()
        {  
            /*  调用方法  */
            Test3 t = new Test3();
            string r1 = t.Process("Text1", "Text2", new ProcessDelegate3(t.Process1));
            string r2 = t.Process("Text1", "Text2", new ProcessDelegate3(t.Process2));
            string r3 = t.Process("Text1", "Text2", new ProcessDelegate3(t.Process3));

            Console.WriteLine(r1);
            Console.WriteLine(r2);
            Console.WriteLine(r3);
        }
        #endregion

        #region 泛型委托详解

        #endregion

    }
    //test10-15见到委托
    delegate void BuyBook();
    //test16
    public delegate string AsyncMethodCaller(int callDuration, out int threadId);
    //test16
    public class AsyncTest
    {
        // The method to be executed asynchronously.
        public string TestMethod(int callDuration, out int threadId)
        {
            Console.WriteLine("Test method begins.");
            Thread.Sleep(callDuration);
            threadId = Thread.CurrentThread.ManagedThreadId;
            return String.Format("My call time was {0}.", callDuration.ToString());
        }
    }
    //test17简单委托
    public delegate string ProcessDelegate1(string s1, string s2);
    //test18泛型委托
    public delegate string ProcessDelegateT<T, S>(T s1, S s2);
    //test17-18
    public class Test
    {
        /// <summary>
        /// 方法
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public string Process1(string s1, string s2)
        {
            return s1 + s2;
        }
        public string Process2(string s1, int s2)
        {
            return s1 + s2;
        }
    }
    //test19委托+事件
    public delegate void ProcessDelegate2(object sender, EventArgs e);
    //test19委托+事件
    public class Test1
    {
        private string s1;

        public string Text1
        {
            get { return s1; }
            set { s1 = value; }
        }

        private string s2;

        public string Text2
        {
            get { return s2; }
            set { s2 = value; }
        }

        //创建一个委托事件
        public event ProcessDelegate2 ProcessEvent;

        void ProcessAction(object sender, EventArgs e)
        {
            if (ProcessEvent == null)
                ProcessEvent += new ProcessDelegate2(t_ProcessEvent);
            ProcessEvent(sender, e);
        }

        //如果没有自己指定关联方法，将会调用该方法抛出错误
        void t_ProcessEvent(object sender, EventArgs e)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void OnProcess()
        {
            ProcessAction(this, EventArgs.Empty);
        }

        public string Process()
        {
            OnProcess();
            return s1 + s2;
        }
    }
    //回调函数
    public delegate string ProcessDelegate3(string s1, string s2);
    //回调函数
    public class Test3
    {
        public string Process(string s1, string s2, ProcessDelegate3 process)
        {
            return process(s1, s2);
        }

        public string Process1(string s1, string s2)
        {
            return s1 + s2;
        }

        public string Process2(string s1, string s2)
        {
            return s1 + Environment.NewLine + s2;
        }

        public string Process3(string s1, string s2)
        {
            return s2 + s1;
        }
    }
}


