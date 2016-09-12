/*
 * Phosphorus Five, copyright 2014 - 2016, Thomas Hansen, phosphorusfive@gmail.com
 * Phosphorus Five is licensed under the terms of the MIT license, see the enclosed LICENSE file for details
 */

using System;
using System.Linq;
using System.Collections.Generic;
using p5.core;

namespace p5.exp.iterators
{
    /// <summary>
    ///     Returns all children of previous iterator
    /// </summary>
    [Serializable]
    public class IteratorChildren : Iterator
    {
        public override IEnumerable<Node> Evaluate (ApplicationContext context)
        {
            return Left.Evaluate (context).SelectMany (idxCurrent => idxCurrent.Children);
        }
    }
}
