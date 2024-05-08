﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command
{
    /// <summary>
    /// Implements the more generic parts of the ITextIoContext
    /// Allows for the implementation to handle the context specific parts
    /// </summary>
    public abstract class AbstractTextIo : ITextIoContext
    {

        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; set; } = String.Empty;

        public Guid? Parent { get; protected set; } = null;

        public bool HasPipedInput { get; private set; } = false;

        public string[] Parameters { get; set; } = Array.Empty<string>();

        protected ChannelReader<string>? inputPipe;
        protected ChannelWriter<string>? outputPipe;
        /// <summary>
        /// constructor requires a name and optional parent guid
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parentId"></param>
        protected AbstractTextIo(string name, Guid? parentId = null)
        {
            Name = name;
            Parent = parentId;
        }

        public abstract Task<ITextIoContext> GetChild(string[]? childArguments = null);
        /// <summary>
        /// handles the Channel output and allows the implementation to handle 
        /// the final output
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Task OutputChunk(string message)
        {
            if (this.outputPipe == null)
            {
                return this.HandleOutputChunk(message);
            }
            return outputPipe.WriteAsync(message).AsTask();
        }
        /// <summary>
        /// allow the implementation to handle the final output
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public abstract Task HandleOutputChunk(string chunk);
        /// <summary>
        /// allow the implementation to handle the prompting for input
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public abstract Task<string> PromptForCommand(string prompt);
        /// <summary>
        /// handles the Channel so the command just handles the await foreach
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<string> ReadInputPipeChunks()
        {
            if (inputPipe == null) yield break;

            await foreach (var item in inputPipe.ReadAllAsync())
            {
                yield return item;
            }
        }

        public void setInputPipe(ChannelReader<string> reader)
        {
            this.HasPipedInput = true;
            this.inputPipe = reader;
        }

        public void SetOutputPipe(ChannelWriter<string> writer)
        {
            this.outputPipe = writer;
        }
        /// <summary>
        /// display progress
        /// </summary>
        /// <param name="total"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public abstract Task<int> SetProgress(int total, int step);
        /// <summary>
        /// display status message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public abstract Task SetStatusMessage(string message);
        /// <summary>
        /// complete the output pipe
        /// </summary>
        /// <returns></returns>
        public ValueTask DisposeAsync()
        {
            this.Complete().Wait();
            return ValueTask.CompletedTask;
        }

        public Task Complete(string? message = null)
        {
            if (!String.IsNullOrEmpty(message)) this.SetStatusMessage(message).Wait();

            this.outputPipe?.TryComplete();
            return Task.CompletedTask;
        }
    }
}
