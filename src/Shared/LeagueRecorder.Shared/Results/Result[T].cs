﻿using System;
using System.Collections.Generic;

namespace LeagueRecorder.Shared.Results
{
    public class Result<T>
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Result{T}"/> class.
        /// This constructor is internal, so the user has to use the factory methods in the <see cref="Result"/> class.
        /// </summary>
        internal Result()
        {
            this.AdditionalData = new Dictionary<string, object>();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the data.
        /// </summary>
        public T Data { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether <see cref="State"/> is <see cref="ResultState.Success"/>.
        /// </summary>
        public bool IsSuccess
        {
            get { return this.State == ResultState.Success; }
        }
        /// <summary>
        /// Gets a value indicating whether <see cref="State"/> is <see cref="ResultState.Warning"/>.
        /// </summary>
        public bool IsWarning
        {
            get { return this.State == ResultState.Warning; }
        }
        /// <summary>
        /// Gets a value indicating whether <see cref="State"/> is <see cref="ResultState.Error"/>.
        /// </summary>
        public bool IsError
        {
            get { return this.State == ResultState.Error; }
        }
        /// <summary>
        /// Gets the more detailed state.
        /// For more simple cases you can use the properties <see cref="IsSuccess"/> and <see cref="IsError"/>.
        /// </summary>
        public ResultState State { get; internal set; }
        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; internal set; }
        /// <summary>
        /// Gets the additional data.
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; internal set; } 
        #endregion

        #region Implicit Operators
        /// <summary>
        /// Transforms a <see cref="Result"/> instance in a generic <see cref="Result{T}"/>.
        /// Be careful, this only works if the <paramref name="result"/> <see cref="Result.IsSuccess"/> property is <c>false</c>.
        /// </summary>
        /// <param name="result">The result.</param>
        public static implicit operator Result<T>(Result result)
        {
            if (result.IsSuccess && typeof(T) != typeof(object))
                throw new InvalidOperationException("You can't return a successfull 'Result' without data.");

            return new Result<T>
            {
                State = result.State,
                Message = result.Message,
                AdditionalData = result.AdditionalData
            };
        }
        /// <summary>
        /// Converts <see cref="Result{T}"/> instance in a non-generic <see cref="Result"/>.
        /// Be careful, the <see cref="Result{T}.Data"/> will get lost.
        /// </summary>
        /// <param name="result">The result.</param>
        public static implicit operator Result(Result<T> result)
        {
            return new Result
            {
                State = result.State,
                Message = result.Message,
                AdditionalData = result.AdditionalData
            };
        }
        #endregion
    }
}