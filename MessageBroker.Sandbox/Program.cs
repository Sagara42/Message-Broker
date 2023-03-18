using NLog.Config;
using NLog.Targets;
using NLog;
using System;
using MessageBroker.Client.Network.Client;

namespace MessageBroker.Sandbox
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LogManager.Configuration = NLogDefaultConfiguration;

            var broker = new Broker("0.0.0.0", 6677);
            broker.Start();

            var client = BrokerClientFactory.GetClient("127.0.0.1", 6677);
            client.Connect();

            client.DeclareTopic("test_a", "/test/a");
            client.DeclareTopic("test_b", "/test/b");

            var test_a_subscription = client.Subscribe("test_a");
            test_a_subscription.OnMessageReceived += (ea) =>
            {
                Console.WriteLine("test_a received");
                
                ea.Ack();

                client.Unsubscribe(test_a_subscription.Id);
                client.DeleteTopic("test_a");
            };

            var test_b_subscription = client.Subscribe("test_b");
            test_b_subscription.OnMessageReceived += (ea) =>
            {
                Console.WriteLine("test_b received");

                ea.Ack();
                client.Unsubscribe(test_b_subscription.Id);
                client.DeleteTopic("test_b");
            };

            client.Publish("/test/*", new byte[] { 0x01, 0x02, 0x03, 0x04 });

            Console.Read();
        }

        public static LoggingConfiguration NLogDefaultConfiguration
        {
            get
            {
                var config = new LoggingConfiguration();

                var consoleTarget = new ColoredConsoleTarget
                {
                    Layout =
                        "${time} | ${message}${onexception:${newline}EXCEPTION OCCURRED${newline}${exception:format=tostring}}",
                    UseDefaultRowHighlightingRules = false
                };
                config.AddTarget("console", consoleTarget);

                var fileTarget = new FileTarget
                {
                    Layout =
                        "${time} | ${message}${onexception:${newline}EXCEPTION OCCURRED${newline}${exception:format=tostring}}",
                    FileName = "${basedir}/Logs/${shortdate}/${level}.txt"
                };
                config.AddTarget("file", fileTarget);

                consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Debug",
                    ConsoleOutputColor.DarkGray,
                    ConsoleOutputColor.Black));
                consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Info",
                    ConsoleOutputColor.White,
                    ConsoleOutputColor.Black));
                consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Warn",
                    ConsoleOutputColor.Yellow,
                    ConsoleOutputColor.Black));
                consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Error",
                    ConsoleOutputColor.Red,
                    ConsoleOutputColor.Black));
                consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule("level == LogLevel.Fatal",
                    ConsoleOutputColor.Red,
                    ConsoleOutputColor.White));

                LoggingRule rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
                config.LoggingRules.Add(rule1);

                LoggingRule rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
                config.LoggingRules.Add(rule2);

                return config;
            }
        }
    }
}