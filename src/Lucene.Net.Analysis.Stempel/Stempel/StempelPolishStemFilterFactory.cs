﻿using Lucene.Net.Analysis.Pl;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Analysis.Stempel
{
    /*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

    /// <summary>
    /// Factory for <see cref="StempelFilter"/> using a Polish stemming table.
    /// </summary>
    public class StempelPolishStemFilterFactory : TokenFilterFactory
    {
        /// <summary>
        /// Creates a new <see cref="StempelPolishStemFilterFactory"/>
        /// </summary>
        public StempelPolishStemFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new StempelFilter(input, new StempelStemmer(PolishAnalyzer.GetDefaultTable()));
        }
    }
}
