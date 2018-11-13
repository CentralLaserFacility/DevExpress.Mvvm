﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
#if !NETFX_CORE
using System.Windows.Controls;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DevExpress.Mvvm.Native;
#if FREE
using DevExpress.Mvvm.UI.Native;
#else
using DevExpress.UI.Xaml.Editors.Native;
#endif
#endif

namespace DevExpress.Mvvm.UI {
    public class EventToCommand : EventToCommandBase {
        public static readonly DependencyProperty EventArgsConverterProperty =
            DependencyProperty.Register("EventArgsConverter", typeof(IEventArgsConverter), typeof(EventToCommand),
            new PropertyMetadata(null));
#if !NETFX_CORE
        public static readonly DependencyProperty PassEventArgsToCommandProperty = 
            DependencyProperty.Register("PassEventArgsToCommand", typeof(bool?), typeof(EventToCommand), new PropertyMetadata(null));
#else
        public static readonly DependencyProperty PassEventArgsToCommandProperty = 
            DependencyProperty.Register("PassEventArgsToCommand", typeof(bool), typeof(EventToCommand), new PropertyMetadata(false));
#endif
        public static readonly DependencyProperty AllowChangingEventOwnerIsEnabledProperty =
            DependencyProperty.Register("AllowChangingEventOwnerIsEnabled", typeof(bool), typeof(EventToCommand),
            new PropertyMetadata(false, (d, e) => ((EventToCommand)d).UpdateIsEnabled()));
        public static readonly DependencyProperty ModifierKeysProperty =
            DependencyProperty.Register("ModifierKeys", typeof(ModifierKeys?), typeof(EventToCommand),
            new PropertyMetadata(null));

        public IEventArgsConverter EventArgsConverter {
            get { return (IEventArgsConverter)GetValue(EventArgsConverterProperty); }
            set { SetValue(EventArgsConverterProperty, value); }
        }
#if !NETFX_CORE
        public bool? PassEventArgsToCommand {
            get { return (bool?)GetValue(PassEventArgsToCommandProperty); }
            set { SetValue(PassEventArgsToCommandProperty, value); }
        }
        protected bool ActualPassEventArgsToCommand {
            get { return PassEventArgsToCommand ?? EventArgsConverter != null; }
        }
#else
        public bool PassEventArgsToCommand {
            get { return (bool)GetValue(PassEventArgsToCommandProperty); }
            set { SetValue(PassEventArgsToCommandProperty, value); }
        }
        protected bool ActualPassEventArgsToCommand {
            get { return PassEventArgsToCommand || EventArgsConverter != null; }
        }
#endif
        public bool AllowChangingEventOwnerIsEnabled {
            get { return (bool)GetValue(AllowChangingEventOwnerIsEnabledProperty); }
            set { SetValue(AllowChangingEventOwnerIsEnabledProperty, value); }
        }
#if NETFX_CORE
        [TypeConverter(typeof(ModifierKeysConverter))]
#endif
        public ModifierKeys? ModifierKeys {
            get { return (ModifierKeys?)GetValue(ModifierKeysProperty); }
            set { SetValue(ModifierKeysProperty, value); }
        }        
        protected override void OnAttached() {
            base.OnAttached();
            UpdateIsEnabled();
        }
        protected override void OnSourceChanged(object oldSource, object newSource) {
            base.OnSourceChanged(oldSource, newSource);
            UpdateIsEnabled();
        }
        protected override void OnCommandChanged(ICommand oldValue, ICommand newValue) {
            base.OnCommandChanged(oldValue, newValue);
            if (oldValue != null)
                oldValue.CanExecuteChanged -= OnCommandCanExecuteChanged;
            if (newValue != null)
                newValue.CanExecuteChanged += OnCommandCanExecuteChanged;
            UpdateIsEnabled();
        }
        protected override void OnCommandParameterChanged(object oldValue, object newValue) {
            base.OnCommandParameterChanged(oldValue, newValue);
            UpdateIsEnabled();
        }
        protected override void Invoke(object sender, object eventArgs) {
            object commandParameter = CommandParameter;
            if(commandParameter == null && ActualPassEventArgsToCommand) {
                if(EventArgsConverter != null)
                    commandParameter = EventArgsConverter.Convert(sender, eventArgs);
                else commandParameter = eventArgs;
            }
            if(Command.CanExecute(commandParameter))
                Command.Execute(commandParameter);
        }
        protected override bool CanInvoke(object sender, object eventArgs) {
            bool res = base.CanInvoke(sender, eventArgs);
            if(ModifierKeys != null)
#if !NETFX_CORE
                res &= ModifierKeys == Keyboard.Modifiers;
#else
                res &= ModifierKeys == ModifierKeysHelper.GetKeyboardModifiers();//TODO check
#endif
                return res;
        }
        void UpdateIsEnabled() {
            if(Command == null) return;
#if !NETFX_CORE
            FrameworkElement associatedFrameworkObject = Source as FrameworkElement;
#else
            Control associatedFrameworkObject = Source as Control;
#endif
            if(AllowChangingEventOwnerIsEnabled && associatedFrameworkObject != null) {
                associatedFrameworkObject.IsEnabled = Command.CanExecute(CommandParameter);
            }
        }
        void OnCommandCanExecuteChanged(object sender, EventArgs e) {
            UpdateIsEnabled();
        }
    }
}
