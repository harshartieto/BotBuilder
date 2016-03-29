﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Bot Framework: http://botframework.com
// 
// Bot Builder SDK Github:
// https://github.com/Microsoft/BotBuilder
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Dialogs
{
    /// <summary>
    /// A fluent, chainable interface for IDialogs.
    /// </summary>
    public static partial class Fluent
    {
        /// <summary>
        /// When the antecedent <see cref="IDialog{T}"/> has completed, execute this continuation method to construct the next <see cref="IDialog{R}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the antecedent dialog.</typeparam>
        /// <typeparam name="R">The type of the next dialog.</typeparam>
        /// <param name="context">The bot context.</param>
        /// <param name="item">The result of the previous <see cref="IDialog{T}"/>.</param>
        /// <returns>A task that represents the next <see cref="IDialog{R}"/>.</returns>
        public delegate Task<IDialog<R>> Continutation<in T, R>(IBotContext context, IAwaitable<T> item);

        /// <summary>
        /// Construct a <see cref="IDialog{T}"/> that will make a new copy of another <see cref="IDialog{T}"/> when started.
        /// </summary>
        /// <typeparam name="T">The type of the dialog.</typeparam>
        /// <param name="MakeDialog">The dialog factory method.</param>
        /// <returns>The new dialog.</returns>
        public static IDialog<T> From<T>(Func<IDialog<T>> MakeDialog)
        {
            return new FromDialog<T>(MakeDialog);
        }

        /// <summary>
        /// When the antecedent <see cref="IDialog{T}"/> has completed, execute the continuation to produce the next <see cref="IDialog{R}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the antecedent dialog.</typeparam>
        /// <typeparam name="R">The type of the next dialog.</typeparam>
        /// <param name="antecedent">The antecedent <see cref="IDialog{T}"/>.</param>
        /// <param name="continuation">The continuation to produce the next <see cref="IDialog{R}"/>.</param>
        /// <returns>The next <see cref="IDialog{R}"/>.</returns>
        public static IDialog<R> ContinueWith<T, R>(this IDialog<T> antecedent, Continutation<T, R> continuation)
        {
            return new ContinueWithDialog<T, R>(antecedent, continuation);
        }

        /// <summary>
        /// Loop the <see cref="IDialog"/> forever.
        /// </summary>
        /// <param name="antecedent">The antecedent <see cref="IDialog"/>.</param>
        /// <returns>The looping dialog.</returns>
        public static IDialog Loop(this IDialog antecedent)
        {
            return new LoopDialog(antecedent);
        }

        [Serializable]
        private sealed class FromDialog<T> : IDialog<T>
        {
            public readonly Func<IDialog<T>> MakeDialog;
            public FromDialog(Func<IDialog<T>> MakeDialog)
            {
                SetField.NotNull(out this.MakeDialog, nameof(MakeDialog), MakeDialog);
            }
            async Task IDialog.StartAsync(IDialogContext context)
            {
                var dialog = this.MakeDialog();
                context.Call<T>(dialog, ResumeAsync);
            }
            private async Task ResumeAsync(IDialogContext context, IAwaitable<T> result)
            {
                context.Done<T>(await result);
            }
        }

        [Serializable]
        private sealed class ContinueWithDialog<T, R> : IDialog<R>
        {
            public readonly IDialog<T> Antecedent;
            public readonly Continutation<T, R> Continuation;
            public ContinueWithDialog(IDialog<T> antecedent, Continutation<T, R> continuation)
            {
                SetField.NotNull(out this.Antecedent, nameof(antecedent), antecedent);
                SetField.NotNull(out this.Continuation, nameof(continuation), continuation);
            }
            async Task IDialog.StartAsync(IDialogContext context)
            {
                context.Call<T>(this.Antecedent, ResumeAsync);
            }
            private async Task ResumeAsync(IDialogContext context, IAwaitable<T> result)
            {
                var next = await this.Continuation(context, result);
                context.Call<R>(next, DoneAsync);
            }
            private async Task DoneAsync(IDialogContext context, IAwaitable<R> result)
            {
                context.Done(await result);
            }
        }

        [Serializable]
        private sealed class LoopDialog : IDialog
        {
            public readonly IDialog Antecedent;
            public LoopDialog(IDialog antecedent)
            {
                SetField.NotNull(out this.Antecedent, nameof(antecedent), antecedent);
            }
            async Task IDialog.StartAsync(IDialogContext context)
            {
                context.Call<object>(this.Antecedent, ResumeAsync);
            }
            private async Task ResumeAsync(IDialogContext context, IAwaitable<object> ignored)
            {
                context.Call<object>(this.Antecedent, ResumeAsync);
            }
        }
    }
}