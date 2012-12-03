﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using umbraco.NodeFactory;
using umbraco;
using System.Collections;

namespace uComponents.Mapping
{
    /// <summary>
    /// Represents a query for mapped Umbraco nodes.  Enumerating the
    /// query gets mapped instances of every node which can be mapped to 
    /// <typeparamref name="TDestination"/>. 
    /// </summary>
    /// <typeparam name="TDestination">
    /// The type which queried nodes will be mapped to.
    /// </typeparam>
    internal class NodeQuery<TDestination> : INodeQuery<TDestination>
        where TDestination : class, new()
    {
        // The paths included in the query
        private readonly List<string> _paths = new List<string>();

        // The engine which will execute the query
        private readonly NodeMappingEngine _engine;

        public NodeQuery(NodeMappingEngine engine)
        {
            if (engine == null)
            {
                throw new ArgumentNullException("engine");
            }

            _engine = engine;
        }

        public INodeMappingEngine Engine
        {
            get
            {
                return _engine;
            }
        }

        public TDestination Map(Node node)
        {
            return (TDestination)_engine.Map(
                node,
                typeof(TDestination),
                _paths.ToArray()
                );
        }

        public TDestination Single(int nodeId)
        {
            return (TDestination)_engine.Map(
                new Node(nodeId),
                typeof(TDestination),
                _paths.ToArray()
                );
        }

        public IEnumerable<TDestination> Many(IEnumerable<int> nodeIds)
        {
            if (nodeIds == null)
            {
                throw new ArgumentNullException("nodeIds");
            }

            return nodeIds.Select(id => Single(id));
        }

        public IEnumerable<TDestination> Many(IEnumerable<Node> nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException("nodes");
            }

            return nodes.Select(n => Map(n));
        }

        public TDestination Current()
        {
            return (TDestination)_engine.Map(
                Node.GetCurrent(),
                typeof(TDestination),
                _paths.ToArray()
                );
        }

        public IEnumerable<TDestination> Explicit()
        {
            var destinationType = typeof(TDestination);

            if (!_engine.NodeMappers.ContainsKey(destinationType))
            {
                throw new MapNotFoundException(destinationType);
            }

            var nodeMapper = _engine.NodeMappers[destinationType];

            return uQuery.GetNodesByType(nodeMapper.SourceNodeTypeAlias)
                .Select(n => (TDestination)_engine.Map(n, destinationType, _paths.ToArray()));
        }

        public INodeQuery<TDestination> Include(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("The path cannot be null or empty", "path");
            }

            if (!_paths.Contains(path))
            {
                _paths.Add(path);
            }

            return this;
        }

        public INodeQuery<TDestination> Include<TProperty>(Expression<Func<TDestination, TProperty>> path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            string parsedPath;
            if (!NodeQueryHelpers.TryParsePath(path.Body, out parsedPath)
                || parsedPath == null)
            {
                throw new ArgumentException(
                    string.Format("Path could not be parsed (got this far: '{0}')", parsedPath),
                    "path"
                    );
            }

            return Include(parsedPath);
        }

        public INodeQuery<TDestination> IncludeMany(string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException("paths");
            }

            foreach (var path in paths)
            {
                this.Include(path);
            }

            return this;
        }

        public INodeQuery<TDestination> IncludeMany(Expression<Func<TDestination, object>>[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException("paths");
            }

            foreach (var path in paths)
            {
                this.Include(path);
            }

            return this;
        }

        #region IEnumerable

        /// <summary>
        /// Gets and enumerator of mapped instances of every node which 
        /// can be mapped to <typeparamref name="TDestination"/>. 
        /// </summary>
        public IEnumerator<TDestination> GetEnumerator()
        {
            var destinationType = typeof(TDestination);

            if (!_engine.NodeMappers.ContainsKey(destinationType))
            {
                throw new MapNotFoundException(destinationType);
            }

            var sourceNodeTypeAliases = _engine.GetCompatibleNodeTypeAliases(destinationType);

            return sourceNodeTypeAliases
                .SelectMany(alias =>
                {
                    var nodes = uQuery.GetNodesByType(alias);

                    return nodes.Select(n => (TDestination)_engine.Map(n, destinationType, _paths.ToArray()));
                })
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    internal static class NodeQueryHelpers
    {

        /// <summary>
        /// Taken from <c>System.Data.Entity.Internal.DbHelpers</c>:
        /// 
        /// Called recursively to parse an expression tree representing a property path..
        /// This involves parsing simple property accesses like o =&gt; o.Products as well as calls to Select like
        /// o =&gt; o.Products.Select(p =&gt; p.OrderLines).
        /// </summary>
        /// <param name="expression"> The expression to parse. </param>
        /// <param name="path"> The expression parsed into an include path, or null if the expression did not match. </param>
        /// <returns> True if matching succeeded; false if the expression could not be parsed. </returns>
        public static bool TryParsePath(Expression expression, out string path)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            path = null;
            var withoutConvert = expression.RemoveConvert(); // Removes boxing
            var memberExpression = withoutConvert as MemberExpression;
            var callExpression = withoutConvert as MethodCallExpression;

            if (memberExpression != null)
            {
                var thisPart = memberExpression.Member.Name;
                string parentPart;
                if (!TryParsePath(memberExpression.Expression, out parentPart))
                {
                    return false;
                }
                path = parentPart == null ? thisPart : (parentPart + "." + thisPart);
            }
            else if (callExpression != null)
            {
                if (callExpression.Method.Name == "Select"
                    && callExpression.Arguments.Count == 2)
                {
                    string parentPart;
                    if (!TryParsePath(callExpression.Arguments[0], out parentPart))
                    {
                        return false;
                    }
                    if (parentPart != null)
                    {
                        var subExpression = callExpression.Arguments[1] as LambdaExpression;
                        if (subExpression != null)
                        {
                            string thisPart;
                            if (!TryParsePath(subExpression.Body, out thisPart))
                            {
                                return false;
                            }
                            if (thisPart != null)
                            {
                                path = parentPart + "." + thisPart;
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes boxing on the expression.
        /// 
        /// Taken from <c>System.Data.Entity.Utilities.ExpressionExtensions</c>.
        /// </summary>
        public static Expression RemoveConvert(this Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            while ((expression != null)
                   && (expression.NodeType == ExpressionType.Convert
                       || expression.NodeType == ExpressionType.ConvertChecked))
            {
                expression = RemoveConvert(((UnaryExpression)expression).Operand);
            }

            return expression;
        }
    }
}