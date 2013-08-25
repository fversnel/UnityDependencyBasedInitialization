﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityDependencyBasedInitialization
{
    public static class Initialization
    {
        public static IDictionary<Type, IInitializeable> CreateInitializableReference(IEnumerable<Component> initializableComponents)
        {
            var componentReference = new Dictionary<Type, IInitializeable>();
            foreach (var initializable in initializableComponents)
            {
                componentReference.Add(initializable.GetType(), initializable as IInitializeable);
            }
            return componentReference;
        }

        public static IList<Component> FindInitializeables(GameObject gameObject)
        {
            var initializeables = new List<Component>();

            var components = gameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                var componentType = component.GetType();
                var interfaces = componentType.GetInterfaces();
                if (interfaces.Contains(typeof(IInitializeable)))
                {
                    initializeables.Add(component);
                }
            }

            return initializeables;
        }
    }

    public interface IDependencyManager
    {
        IEnumerable<Type> GetExecutionOrder(Type someType);
    }

    public static class DependencyGraph
    {
        public static Node<Type> CreateDependencyGraph(Type someType)
        {
            var dependencies = DependsOn.ExtractDependencies(someType);
            var children = new List<Node<Type>>();
            foreach (Type dependency in dependencies)
            {
                children.Add(CreateDependencyGraph(dependency));
            }
            return new Node<Type>(someType, children);
        }

        public static IEnumerable<T> ExecutionOrder<T>(Node<T> graph)
        {
            return DepthFirstEvaluationOrder(graph)
                .Reverse()
                .Select(node => node.Value);
        }

        private static IEnumerable<Node<T>> DepthFirstEvaluationOrder<T>(Node<T> root)
        {
            Func<Stack<Node<T>>, Node<T>, Stack<Node<T>>> inner = null;
            inner = (order, node) =>
            {
                foreach (var child in node.Children)
                {
                    order = inner(order, child);
                }
                order.Push(node);
                return order;
            };

            return inner(new Stack<Node<T>>(), root);
        } 

        public class Node<T> : IEquatable<Node<T>>
        {
            private readonly T _value;
            private readonly IEnumerable<Node<T>> _children;

            public Node(T value, IEnumerable<Node<T>> children)
            {
                _value = value;
                _children = children;
            }

            public IEnumerable<Node<T>> Children
            {
                get { return _children; }
            }

            public T Value
            {
                get { return _value; }
            }

            public bool Equals(Node<T> other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return EqualityComparer<T>.Default.Equals(_value, other._value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Node<T>) obj);
            }

            public override int GetHashCode()
            {
                return EqualityComparer<T>.Default.GetHashCode(_value);
            }
        }
    }

    public interface IInitializeable
    {
        /// <summary>
        /// Use this for initialization instead of Awake() and Start()
        /// </summary>
        void Initialize();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DependsOn : Attribute
    {
        private readonly Type _dependencyType;

        public DependsOn(Type dependencyType)
        {
            _dependencyType = dependencyType;
        }

        public Type DependencyType
        {
            get { return _dependencyType; }
        }

        public static IEnumerable<Type> ExtractDependencies(Type someType)
        {
            var attributes = someType.GetCustomAttributes(typeof(DependsOn), true).Cast<DependsOn>();
            return attributes.Select(attribute => attribute.DependencyType);
        }
    }
}