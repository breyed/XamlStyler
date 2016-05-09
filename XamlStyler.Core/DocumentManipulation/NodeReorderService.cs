﻿// © Xavalon. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Xavalon.XamlStyler.Core.DocumentManipulation
{
    public class NodeReorderService : IProcessElementService
    {
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Name of parents to reorder children for
        /// </summary>
        public List<NameSelector> ParentNodeNames { get; }

        /// <summary>
        /// Name of children to reorder
        /// </summary>
        public List<NameSelector> ChildNodeNames { get; }

        /// <summary>
        /// Description on how to sort children
        /// </summary>
        public List<SortBy> SortByAttributes { get; }

        public NodeReorderService()
        {
            this.IsEnabled = true;
            this.ParentNodeNames = new List<NameSelector>();
            this.ChildNodeNames = new List<NameSelector>();
            this.SortByAttributes = new List<SortBy>();
        }

        public void ProcessElement(XElement element)
        {
            if (!this.IsEnabled)
            {
                return;
            }

            if (!element.HasElements)
            {
                return;
            }

            if (this.ParentNodeNames.Any(_ => _.IsMatch(element.Name)))
            {
                this.ReorderChildNodes(element);
            }
        }

        /// <summary>
        /// Reorder child nodes matching ChildNodeNames
        /// </summary>
        /// <param name="element">Element thats getting its children reordered</param>
        private void ReorderChildNodes(XElement element)
        {
            List<NodeCollection> nodeCollections = new List<NodeCollection>();

            var children = element.Nodes();

            // This indicates if last element matched ChildNodeNames
            bool inMatchingChildBlock = false;

            // This value changes each time a non matching ChildNodeName is reached ensuring that only sortable elements are reordered
            int childBlockIndex = 0;

            NodeCollection currentNodeCollection = null;

            // Run through children
            foreach (var child in children)
            {
                if (currentNodeCollection == null)
                {
                    currentNodeCollection = new NodeCollection();
                    nodeCollections.Add(currentNodeCollection);
                }

                if (child.NodeType == XmlNodeType.Element)
                {
                    XElement childElement = (XElement)child;

                    var isMatchingChild = this.ChildNodeNames.Any(match => match.IsMatch(childElement.Name));
                    if (isMatchingChild == false || inMatchingChildBlock == false)
                    {
                        childBlockIndex++;
                        inMatchingChildBlock = isMatchingChild;
                    }

                    if (isMatchingChild)
                    {
                        currentNodeCollection.SortAttributeValues = this.SortByAttributes.Select(x => x.GetValue(childElement)).ToArray();
                    }

                    currentNodeCollection.BlockIndex = childBlockIndex;
                }

                currentNodeCollection.Nodes.Add(child);

                if (child.NodeType == XmlNodeType.Element)
                {
                    currentNodeCollection = null;
                }
            }

            if (currentNodeCollection != null)
            {
                currentNodeCollection.BlockIndex = childBlockIndex + 1;
            }

            // sort node list
            nodeCollections = nodeCollections.OrderBy(x => x).ToList();

            // replace the element's nodes
            element.ReplaceNodes(nodeCollections.SelectMany(nc => nc.Nodes));
        }
    }
}