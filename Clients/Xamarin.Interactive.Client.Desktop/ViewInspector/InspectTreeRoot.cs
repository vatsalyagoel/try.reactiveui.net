﻿//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Client.ViewInspector
{
    class InspectTreeRoot : ObservableCollection<InspectTreeNode>
    {
        private ViewInspectorViewModel model;

        public InspectTreeRoot (ViewInspectorViewModel model)
        {
            this.model = model;
        }

        public static InspectTreeRoot CreateRoot (ViewInspectorViewModel model)
        {
            var tree = new InspectTreeRoot (model);
            void ModelPropertyChanged (object sender, PropertyChangedEventArgs args)
            {
                var viewModel = sender as ViewInspectorViewModel;
                switch (args.PropertyName) {
                case nameof (ViewInspectorViewModel.RootView):
                    if (tree.RootNode?.View == viewModel.RootView)
                        return;

                    tree.RootNode = new InspectTreeNode (null, viewModel.RootView);
                    break;
                case nameof (ViewInspectorViewModel.RepresentedView):
                    InspectTreeNode found = null;
                    if (tree.RepresentedNode?.View == viewModel.RepresentedView)
                        return;

                    foreach (var node in tree.RootNode.TraverseTree<TreeNode> (n => n.Children)) {
                        if (node.RepresentedObject == viewModel.RepresentedView) {
                            found = node as InspectTreeNode;
                            break;
                        }
                    }
                    tree.RepresentedNode = found;
                    break;
                case nameof (ViewInspectorViewModel.SelectedView):
                    if (tree.selectedNode?.View == viewModel.SelectedView)
                        return;

                    foreach (var node in tree.RootNode.TraverseTree<TreeNode> (n => n.Children)) {
                        var selected = node.RepresentedObject == viewModel.SelectedView;
                        node.IsSelected = selected;
                        if (node.IsSelected) {
                            tree.SelectedNode = node as InspectTreeNode;
                            var parent = (node as InspectTreeNode).Parent;
                            while (parent != null) {
                                parent.IsExpanded = true;
                                parent = parent.Parent;
                            }
                        }
                    }
                    break;
                case nameof (ViewInspectorViewModel.RenderingDepth):
                case nameof (ViewInspectorViewModel.DisplayMode):
                case nameof (ViewInspectorViewModel.ShowHidden):
                    break;
                }
            }
            model.PropertyChanged += ModelPropertyChanged;
            return tree;
        }

        //It might be nice to consume the fake root node here when possible
        InspectTreeNode rootNode;
        public InspectTreeNode RootNode {
            get => rootNode;
            set {
                Clear ();

                if (value == null)
                    return;

                rootNode = value;
                if (rootNode.View.IsFakeRoot)
                    foreach (var node in rootNode.Children.OfType<InspectTreeNode> ())
                        Add (node);
                else
                    Add (value);

                NotifyPropertyChanged ();
            }
        }

        InspectTreeNode represented;
        public InspectTreeNode RepresentedNode {
            private set {
                if (represented != value) {
                    represented = value;
                    NotifyPropertyChanged ();
                }
            }
            get => represented;
        }

        InspectTreeNode selectedNode;
        public InspectTreeNode SelectedNode {
            set {
                if (value == selectedNode && model.SelectedView == selectedNode?.View)
                    return;

                selectedNode = value;
                model.SelectedView = selectedNode?.View;
                NotifyPropertyChanged ();
            }
            get {
                return selectedNode;
            }
        }

        protected virtual void NotifyPropertyChanged ([CallerMemberName]string name = null) =>
            OnPropertyChanged (new PropertyChangedEventArgs (name));
    }
}
