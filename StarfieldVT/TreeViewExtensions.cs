using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace StarfieldVT
{
    public static class TreeViewExtensions
    {
        public static void ExpandAll(this TreeView treeView)
        {
            foreach (var item in treeView.Items)
            {
                if (treeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeItem)
                {
                    treeItem.ExpandSubtree();
                }
            }
        }

        /// <summary>
        /// Applies a search filter to all items of a TreeView recursively
        /// </summary>
        public static void Filter(this TreeView self, Predicate<object> predicate)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(self.ItemsSource);
            if (view == null)
                return;
            view.Filter = predicate;
            foreach (var obj in self.Items)
            {
                var item = self.ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                FilterRecursively(self, item, predicate);
            }
        }

        private static void FilterRecursively(TreeView tree, TreeViewItem item, Predicate<object> predicate)
        {
            if (item == null) return;
            ICollectionView view = CollectionViewSource.GetDefaultView(item.ItemsSource);
            if (view == null)
                return;
            view.Filter = predicate;
            foreach (var obj in item.Items)
            {
                var childItem = tree.ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                FilterRecursively(tree, childItem, predicate);
            }
        }
    }
}
