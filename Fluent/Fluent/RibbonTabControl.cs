﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Input;

namespace Fluent
{
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(RibbonTabItem))]
    [TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
    public class RibbonTabControl: Selector
    {
        #region Constants
        

        #endregion

        #region Dependency propeties
        
        private static readonly DependencyPropertyKey SelectedContentPropertyKey = DependencyProperty.RegisterReadOnly("SelectedContent", typeof(object), typeof(RibbonTabControl), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty SelectedContentProperty = SelectedContentPropertyKey.DependencyProperty;
        public static readonly DependencyProperty IsMinimizedProperty = DependencyProperty.Register("IsMinimized", typeof(bool), typeof(RibbonTabControl), new UIPropertyMetadata(false, OnMinimizedChanged));
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register("IsOpen", typeof(bool), typeof(RibbonTabControl), new UIPropertyMetadata(false, OnIsOpenChanged));

        #endregion

        #region Fields

        private Popup popup = null;

        private object oldSelectedItem = null;

        #endregion

        #region Свойства

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedContent
        {
            get
            {
                return base.GetValue(SelectedContentProperty);
            }
            internal set
            {
                base.SetValue(SelectedContentPropertyKey, value);
            }
        }

        public bool IsMinimized
        {
            get { return (bool)GetValue(IsMinimizedProperty); }
            set { SetValue(IsMinimizedProperty, value); }
        }

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        #endregion

        #region Инициализация

        static RibbonTabControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonTabControl), new FrameworkPropertyMetadata(typeof(RibbonTabControl)));

            EventManager.RegisterClassHandler(typeof(RibbonTabControl), Mouse.PreviewMouseDownOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));
            EventManager.RegisterClassHandler(typeof(RibbonTabControl), Mouse.PreviewMouseUpOutsideCapturedElementEvent, new MouseButtonEventHandler(OnClickThroughThunk));
        }                

        public RibbonTabControl()
        {            
        }

        #endregion

        #region Overrides

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            //base.CanSelectMultiple = false;
            base.ItemContainerGenerator.StatusChanged += OnGeneratorStatusChanged;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new RibbonTabItem();
        }

        /// <summary>
        /// On new style applying
        /// </summary>
        public override void OnApplyTemplate()
        {
            popup = GetTemplateChild("PART_Popup") as Popup;

            /*contentPresenter = FindName(SelectedContentHostTemplateName) as ContentPresenter;
            if (contentPresenter == null) throw new Exception("Incorrect control template.");*/
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is RibbonTabItem);
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if ((e.Action == NotifyCollectionChangedAction.Remove) && (base.SelectedIndex == -1))
            {
                int startIndex = e.OldStartingIndex + 1;
                if (startIndex > base.Items.Count)
                {
                    startIndex = 0;
                }
                RibbonTabItem item = this.FindNextTabItem(startIndex, -1);
                if (item != null)
                {
                    item.IsSelected = true;
                }                
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if (e.AddedItems.Count > 0)
            {
                if (IsMinimized)
                {
                    if (oldSelectedItem == e.AddedItems[0])
                        IsOpen = !IsOpen;
                    else IsOpen = true;
                }
                this.UpdateSelectedContent();
            }
            if (e.RemovedItems.Count > 0) oldSelectedItem = e.RemovedItems[0];
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (Mouse.Captured != this)
            {
                UIElement selectedTabGroupsPopupChild = popup.Child;
                if (e.OriginalSource == this)
                {
                    // If Ribbon loses capture because something outside popup is clicked - close the popup
                    if (Mouse.Captured == null || !selectedTabGroupsPopupChild.IsAncestorOf(Mouse.Captured as DependencyObject))
                    {
                        this.IsOpen = false;
                    }
                }
                else
                {
                    // If control inside Ribbon loses capture - restore capture to Ribbon
                    if (selectedTabGroupsPopupChild.IsAncestorOf(e.OriginalSource as DependencyObject))
                    {
                        if (this.IsOpen && Mouse.Captured == null)
                        {
                            Mouse.Capture(this, CaptureMode.SubTree);
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        this.IsOpen = false;
                    }
                }
            }
            base.OnLostMouseCapture(e);
        }

        #endregion

        #region Private methods

        // Get selected ribbon tab item
        private RibbonTabItem GetSelectedTabItem()
        {
            object selectedItem = base.SelectedItem;
            if (selectedItem == null)
            {
                return null;
            }
            RibbonTabItem item = selectedItem as RibbonTabItem;
            if (item == null)
            {
                item = base.ItemContainerGenerator.ContainerFromIndex(base.SelectedIndex) as RibbonTabItem;
            }
            return item;
        }

        private RibbonTabItem FindNextTabItem(int startIndex, int direction)
        {
            if (direction != 0)
            {
                int index = startIndex;
                for (int i = 0; i < base.Items.Count; i++)
                {
                    index += direction;
                    if (index >= base.Items.Count)
                    {
                        index = 0;
                    }
                    else if (index < 0)
                    {
                        index = base.Items.Count - 1;
                    }
                    RibbonTabItem item2 = base.ItemContainerGenerator.ContainerFromIndex(index) as RibbonTabItem;
                    if (((item2 != null) && item2.IsEnabled) && (item2.Visibility == Visibility.Visible))
                    {
                        return item2;
                    }
                }
            }
            return null;
        }

        private void UpdateSelectedContent()
        {
            if (base.SelectedIndex < 0)
            {
                this.SelectedContent = null;
            }
            else
            {
                RibbonTabItem selectedTabItem = this.GetSelectedTabItem();
                if (selectedTabItem != null)
                {
                    FrameworkElement parent = VisualTreeHelper.GetParent(selectedTabItem) as FrameworkElement;
                    this.SelectedContent = selectedTabItem.Content;
                }
            }
        }

        #endregion

        #region Event handling

        private void OnGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (base.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                //if (base.HasItems && (base._selectedItems.Count == 0))
                if (base.HasItems && (base.SelectedIndex == -1))
                {
                    base.SelectedIndex = 0;
                }
                this.UpdateSelectedContent();
            }
        }

        private static void OnMinimizedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonTabControl tab = d as RibbonTabControl;
            if(!tab.IsMinimized) tab.IsOpen = false;
            else if (Mouse.Captured == tab)
            {
                Mouse.Capture(null);
            }
        }

        private static void OnClickThroughThunk(object sender, MouseButtonEventArgs e)
        {
            RibbonTabControl ribbon = (RibbonTabControl)sender;
            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
            {
                if (Mouse.Captured == ribbon)
                {
                    ribbon.IsOpen = false;
                    Mouse.Capture(null);
                }
            }
        }

        private void OnRibbonTabPopupClosing()
        {
            if (Mouse.Captured == this)
            {
                Mouse.Capture(null);
            }
        }

        private void OnRibbonTabPopupOpening()
        {
            if (IsMinimized)
            {
                Mouse.Capture(this, CaptureMode.SubTree);
            }
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RibbonTabControl ribbon = (RibbonTabControl)d;

            if (ribbon.IsOpen)
            {
                ribbon.OnRibbonTabPopupOpening();
            }
            else
            {
                ribbon.OnRibbonTabPopupClosing();
            }
        }

        #endregion
    }
}