﻿//Copyright 2014 Spin Services Limited

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System.Collections.Generic;

namespace SS.Integration.Adapter.Model.Interfaces
{
    /// <summary>
    /// A IMarketStateCollection is a container for IMarketState objects.
    /// Its purpose is to model markets' states by updating with
    /// information coming from subsequents snapshots
    /// </summary>
    public interface IMarketStateCollection
    {
        /// <summary>
        /// Return true if this collection contains
        /// the market with the given MarketId
        /// </summary>
        /// <param name="MarketId"></param>
        /// <returns></returns>
        bool HasMarket(string MarketId);

        /// <summary>
        /// Returns the IMarketState associated with
        /// the given MarketId
        /// </summary>
        /// <param name="MarketId"></param>
        /// <returns></returns>
        IMarketState this[string MarketId] { get; }

        /// <summary>
        /// Returns the list of marktets' ids
        /// currently contained within this collection
        /// </summary>
        IEnumerable<string> Markets { get; }

        /// <summary>
        /// Returns the number of markets
        /// contained within this collection
        /// </summary>
        int MarketCount { get; }
    }
}