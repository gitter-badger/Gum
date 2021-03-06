﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.Wireframe;

namespace Gum.Managers
{
    public partial class StateTreeViewManager
    {
        #region Fields

        static StateTreeViewManager mSelf;

        MultiSelectTreeView mTreeView;

        IStateContainer mLastElementRefreshedTo;
        ContextMenuStrip mMenuStrip;

        #endregion

        #region Properties

        public static StateTreeViewManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new StateTreeViewManager();
                }
                return mSelf;
            }
        }

        public TreeNode SelectedNode
        {
            get { return mTreeView.SelectedNode; }
        }

        #endregion

        public TreeNode GetTreeNodeForTag(object tag)
        {
            if (tag == null)
            {
                return null;
            }
            // Will need to expand this when we add categories
            foreach (TreeNode node in mTreeView.Nodes)
            {
                if (node.Tag == tag)
                {
                    return node;
                }

                foreach (TreeNode subnode in node.Nodes)
                {
                    if (subnode.Tag == tag)
                    {
                        return subnode;
                    }
                }
            }
            return null;
        }



        public void Initialize(MultiSelectTreeView treeView, ContextMenuStrip menuStrip)
        {
            mMenuStrip = menuStrip;
            mTreeView = treeView;


            mMenuStrip.Items.Clear();

            var tsmi = new ToolStripMenuItem();
            tsmi.Text = "Add State";
            tsmi.Click += ((obj, arg) =>
            {

                GumCommands.Self.Edit.AddState();
            });
            mMenuStrip.Items.Add(tsmi);

            tsmi = new ToolStripMenuItem();
            tsmi.Text = "Add Category";
            tsmi.Click += ((obj, arg) =>
            {

                GumCommands.Self.Edit.AddCategory();
            });
            mMenuStrip.Items.Add(tsmi);
        }

        internal void OnSelect()
        {

            TreeNode treeNode = mTreeView.SelectedNode;

            object selectedObject = null;

            if (treeNode != null)
            {
                selectedObject = treeNode.Tag;
            }

            if (selectedObject == null)
            {
                // What do we do?  This is invalid.  A State should always be selected...
                // What we do is select the first one if it exists
                if (mTreeView.Nodes.Count != 0)
                {
                    var newlySelectedNode = mTreeView.Nodes.FirstOrDefault(item=> 
                        {
                            TreeNode itemAsNode = item as TreeNode;
                            return itemAsNode.Tag is StateSave && (itemAsNode.Tag as StateSave).Name == "Default";
                        }) as TreeNode;

                    selectedObject = newlySelectedNode?.Tag;
                    mTreeView.SelectedNode = newlySelectedNode;
                }
            }
            SelectedState.Self.CustomCurrentStateSave = null;
            SelectedState.Self.UpdateToSelectedStateSave();
            // refreshes the yellow highlights
            StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedStateContainer);
        }

        public void Select(StateSave stateSave)
        {
            TreeNode treeNode = GetTreeNodeForTag(stateSave);

            Select(treeNode);

        }

        public void Select(StateSaveCategory stateSaveCategory)
        {
            TreeNode treeNode = GetTreeNodeForTag(stateSaveCategory);

            Select(treeNode);
        }

        public void Select(TreeNode treeNode)
        {
            if (mTreeView.SelectedNode != treeNode)
            {
                mTreeView.SelectedNode = treeNode;

                if (treeNode != null)
                {
                    treeNode.EnsureVisible();
                }
            }
        }

        public void RefreshUI(IStateContainer stateContainer)
        {

            bool changed = stateContainer != mLastElementRefreshedTo;

            mLastElementRefreshedTo = stateContainer;

            StateSave lastStateSave = SelectedState.Self.SelectedStateSave;
            InstanceSave instance = SelectedState.Self.SelectedInstance;


            if (stateContainer != null)
            {
                RemoveUnnecessaryNodes(stateContainer);

                AddNeededNodes(stateContainer);

                FixNodeOrder(stateContainer);

                bool wasAnythingSelected = false;

                foreach(var state in stateContainer.AllStates)
                {
                    wasAnythingSelected = UpdateStateTreeNode(lastStateSave, instance, wasAnythingSelected, state);
                }

                foreach(var category in stateContainer.Categories)
                {
                    UpdateCategoryTreeNode(category);
                }

            }
            else
            {
                mTreeView.Nodes.Clear();
            }


        }

        private TreeNodeCollection ParentOf(TreeNode node)
        {
            var toReturn = mTreeView.Nodes;

            if(node.Parent != null)
            {
                toReturn = node.Parent.Nodes;
            }

            return toReturn;
        }

        private void FixNodeOrder(IStateContainer stateContainer)
        {
            // first make sure categories come first
            int desiredIndex = 0;

            foreach(var category in stateContainer.Categories.OrderBy(item=>item.Name))
            {
                var node = GetTreeNodeForTag(category);

                var parent = ParentOf(node);

                var nodeIndex = parent.IndexOf(node);

                if(nodeIndex != desiredIndex)
                {
                    parent.Remove(node);
                    parent.Insert(desiredIndex, node);
                }


                FixNodeOrderInCategory(category);

                desiredIndex++;
            }

            // do uncategorized states
            for (int i = 0; i < stateContainer.UncategorizedStates.Count(); i++)
            {
                
                var state = stateContainer.UncategorizedStates.ElementAt(i);

                var node = GetTreeNodeForTag(state);

                var parent = ParentOf(node);

                int nodeIndex = parent.IndexOf(node);


                if(nodeIndex != desiredIndex)
                {
                    parent.Remove(node);
                    parent.Insert(desiredIndex, node);
                }

                desiredIndex++;
            }


        }

        private void FixNodeOrderInCategory(StateSaveCategory category)
        {
            for(int i = 0; i< category.States.Count; i++)
            {
                var state = category.States[i];
                var node = GetTreeNodeForTag(state);

                var parent = ParentOf(node);

                var nodeIndex = parent.IndexOf(node);

                if(nodeIndex != i)
                {
                    parent.Remove(node);
                    parent.Insert(i, node);
                }
            }
        }

        private void UpdateCategoryTreeNode(StateSaveCategory category)
        {
            var node = GetTreeNodeForTag(category);

            if (node.Text != category.Name)
            {
                node.Text = category.Name;
            }
        }

        private bool UpdateStateTreeNode(StateSave lastStateSave, InstanceSave instance, bool wasAnythingSelected, StateSave state)
        {
            string stateName = state.Name;
            if (string.IsNullOrEmpty(stateName))
            {
                stateName = "Default";
            }

            var node = GetTreeNodeForTag(state);

            if (node.Text != stateName)
            {
                node.Text = stateName;
            }
            if (node.Tag != state)
            {
                node.Tag = state;
            }

            node.ImageIndex = ElementTreeViewManager.StateImageIndex;

            if (state == lastStateSave)
            {

                SelectedState.Self.SelectedStateSave = state;

                wasAnythingSelected = true;
            }
            else if (!node.IsSelected && mTreeView.SelectedNode != node)
            {
                System.Drawing.Color desiredColor = System.Drawing.Color.White;
                if (instance != null && state.Variables.Any(item => item.Name.StartsWith(instance.Name + ".")))
                {
                    desiredColor = System.Drawing.Color.Yellow;
                }

                if (node.BackColor != desiredColor)
                {
                    node.BackColor = desiredColor;
                }
            }

            return wasAnythingSelected;
        }

        private void AddNeededNodes(IStateContainer stateContainer)
        {
            foreach (var category in stateContainer.Categories)
            {
                if (GetTreeNodeForTag(category) == null)
                {
                    var treeNode = mTreeView.Nodes.Add(category.Name);
                    treeNode.Tag = category;
                    treeNode.ImageIndex = ElementTreeViewManager.FolderImageIndex;
                }
            }

            foreach (var state in stateContainer.UncategorizedStates)
            {
                // uncategorized
                if (GetTreeNodeForTag(state) == null)
                {
                    var treeNode = mTreeView.Nodes.Add(state.Name);
                    treeNode.Tag = state;
                    treeNode.ImageIndex = ElementTreeViewManager.StateImageIndex;
                }
            }

            foreach (var category in stateContainer.Categories)
            {
                foreach (var state in category.States)
                {
                    // uncategorized
                    if (GetTreeNodeForTag(state) == null)
                    {
                        var toAddTo = GetTreeNodeForTag(category);

                        var treeNode = toAddTo.Nodes.Add(state.Name);
                        treeNode.ImageIndex = ElementTreeViewManager.StateImageIndex;

                        treeNode.Tag = state;
                    }
                }
            }

        }

        private void RemoveUnnecessaryNodes(IStateContainer stateContainer)
        {
            var allNodes = mTreeView.Nodes.AllNodes().ToList();

            foreach (var node in allNodes)
            {
                if (node.Tag is StateSave)
                {
                    // First check to see if this doesn't exist at all...
                    bool shouldRemove = stateContainer.AllStates.Contains(node.Tag as StateSave) == false;

                    // ... and if it does exist, see if it's part of the wrong category
                    if(!shouldRemove)
                    {
                        if(node.Parent != null)
                        {
                            var category = node.Parent.Tag as StateSaveCategory;

                            if(!category.States.Contains(node.Tag as StateSave))
                            {
                                shouldRemove = true;
                            }
                        }
                        else
                        {
                            shouldRemove = stateContainer.Categories.Any(item => item.States.Contains(node.Tag as StateSave));
                        }
                    }

                    if(shouldRemove)
                    {
                        var parent = ParentOf(node);

                        parent.Remove(node);
                    }
                }
                else if (node.Tag is StateSaveCategory && stateContainer.Categories.Contains(node.Tag as StateSaveCategory) == false)
                {
                    if (node.Parent == null)
                    {
                        mTreeView.Nodes.Remove(node);
                    }
                    else
                    {
                        node.Parent.Nodes.Remove(node);
                    }
                }
            }
        }

        internal bool TryHandleCmdKey(Keys keyData)
        {

            switch (keyData)
            {
                // CTRL+F, control f, search focus, ctrl f, ctrl + f
                case Keys.Alt | Keys.Up:

                    MoveStateInDirection(-1);
                    return true;

                case Keys.Alt | Keys.Down:
                    var stateSave = ProjectState.Self.Selected.SelectedStateSave;
                    bool isDefault = stateSave != null &&
                        stateSave == ProjectState.Self.Selected.SelectedElement.DefaultState;

                    if(!isDefault)
                    {
                        MoveStateInDirection(1);
                    }
                    return true;
                //case Keys.Alt | Keys.Shift | Keys.Down:
                //    return RightClickHelper.MoveToBottom();
                //case Keys.Alt | Keys.Shift | Keys.Up:
                //    return RightClickHelper.MoveToTop();
                default:
                    return false;
            }

        }

        internal void HandleKeyDown(KeyEventArgs e)
        {
            HandleCopyCutPaste(e);
        }

        private void HandleCopyCutPaste(KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Control) == Keys.Control)
            {
                // copy, ctrl c, ctrl + c
                if (e.KeyCode == Keys.C)
                {
                    EditingManager.Self.OnCopy(CopyType.State);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                // paste, ctrl v, ctrl + v
                else if (e.KeyCode == Keys.V)
                {
                    EditingManager.Self.OnPaste(CopyType.State);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                //// cut, ctrl x, ctrl + x
                //else if (e.KeyCode == Keys.X)
                //{
                //    EditingManager.Self.OnCut(CopyType.Instance);
                //    e.Handled = true;
                //    e.SuppressKeyPress = true;
                //}
            }
        }
    }


    static class TreeNodeExtensions
    {
        public static IEnumerable<TreeNode> AllNodes(this TreeNodeCollection treeNodes)
        {
            foreach (TreeNode node in treeNodes)
            {
                foreach (var item in node.Nodes.AllNodes())
                {
                    yield return item;
                }

                yield return node;
            }
        }


    }


}
