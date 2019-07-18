namespace JK.JsonFilter.App
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading;
    using System.Threading.Tasks;
    using JK.JsonFilter.Lib;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Extensions.DependencyInjection;

    public class Program
    {
        private readonly IConsole console;
        private readonly CancellationTokenSource cancellationTokenSource;

        public Program(
            IConsole console,
            CancellationTokenSource cancellationTokenSource)
        {
            this.console = console;
            this.cancellationTokenSource = cancellationTokenSource;

            this.console.CancelKeyPress += this.OnCancelKeyPress;
        }

        private enum ErrorCode
        {
            Success = 0,
            UnknownError = -1,
            OperationCanceled = -2,
        }

        [Option("-s|--selector", Description = "The JSON path field selector. Elements matching the selector will be included in the output.")]
        [Required]
        public string[] JsonFieldSelectors { get; }

        public static int Main(string[] args)
        {
            var serviceProvider = BuildServiceProvider();

            var app = new CommandLineApplication<Program>(serviceProvider.GetRequiredService<IConsole>());

            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(serviceProvider);

            app.OnExecute(app.Model.OnExecuteAsync);

            return app.Execute(args);
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddSingleton(PhysicalConsole.Singleton);
            services.AddSingleton<CancellationTokenSource>();

            return services.BuildServiceProvider();
        }

        private async Task<int> OnExecuteAsync()
        {
            try
            {
                await JsonFilter.FilterJsonAsync(this.console.In, this.console.Out, this.JsonFieldSelectors).ConfigureAwait(false);

                return (int)ErrorCode.Success;
            }
            catch (OperationCanceledException)
            {
                return (int)ErrorCode.OperationCanceled;
            }
            catch (Exception ex)
            {
                await this.WriteExceptionAsync(ex).ConfigureAwait(false);

                return (int)ErrorCode.UnknownError;
            }
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            this.console.Error.WriteLine("Operation cancellation requested.");
            this.cancellationTokenSource.Cancel();
        }

        private async Task WriteExceptionAsync(Exception ex)
        {
            await this.console.Error.WriteLineAsync($"{ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);

            foreach (var dataKey in ex.Data.Keys)
            {
                await this.console.Error.WriteLineAsync($"{dataKey}: {ex.Data[dataKey]}").ConfigureAwait(false);
            }

            await this.console.Error.WriteLineAsync($"StackTrace:{Environment.NewLine}{ex.StackTrace}").ConfigureAwait(false);
        }
    }
}
