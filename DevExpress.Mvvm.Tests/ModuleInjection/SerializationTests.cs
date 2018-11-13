﻿#if !FREE
using DevExpress.Utils.Serializing;
using DevExpress.Xpf.Core.Serialization;
using DevExpress.Xpf.Core.Tests;
#endif
using DevExpress.Mvvm.ModuleInjection;
using DevExpress.Mvvm.ModuleInjection.Native;
using DevExpress.Mvvm.POCO;
using NUnit.Framework;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DevExpress.Mvvm.UI.ModuleInjection.Tests {
    [TestFixture]
    public class LogicalSerializationTests : BaseWpfFixture {
        public class VMTest : ISupportState<VMTest> {
            public string Value { get; set; }
            void ISupportState<VMTest>.RestoreState(VMTest state) { Value = state.Value; }
            VMTest ISupportState<VMTest>.SaveState() { return this; }
        }
        public IModuleManager Manager { get { return ModuleManager.DefaultManager; } }
        public IModuleWindowManager WindowManager { get { return ModuleManager.DefaultWindowManager; } }
        protected override void SetUpCore() {
            base.SetUpCore();
            InjectionTestHelper.SetUp(typeof(LogicalSerializationTests).Assembly);
        }
        protected override void TearDownCore() {
            InjectionTestHelper.TearDown();
            base.TearDownCore();
        }

        [Test]
        public void SerializeEmpty() {
            string logicalState = null;
            string visualState = null;
            LogicalInfo logicalInfo = null;
            VisualInfo visualInfo = null;

            Manager.Save(out logicalState, out visualState);
            logicalInfo = LogicalInfo.Deserialize(logicalState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(0, logicalInfo.Regions.Count());
            Assert.AreEqual(0, visualInfo.Regions.Count());

            Manager.Restore(null, null);
            Manager.Restore(string.Empty, string.Empty);
            Manager.Restore(logicalState, visualState);
        }
        [Test]
        public void SerializeEmpty2() {
            string logicalState = null;
            string visualState = null;
            LogicalInfo logicalInfo = null;
            VisualInfo visualInfo = null;

            Manager.Register("R", new Module("1", () => new object()));

            Manager.Save(out logicalState, out visualState);
            logicalInfo = LogicalInfo.Deserialize(logicalState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(0, logicalInfo.Regions.Count());
            Assert.AreEqual(0, visualInfo.Regions.Count());
        }
        [Test]
        public void SerializeEmpty3() {
            string logicalState = null;
            string visualState = null;
            LogicalInfo logicalInfo = null;
            VisualInfo visualInfo = null;

            Manager.Register("R", new Module("1", () => new object()));
            Manager.Inject("R", "1");

            Manager.Save(out logicalState, out visualState);
            logicalInfo = LogicalInfo.Deserialize(logicalState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(1, logicalInfo.Regions.Count());
            Assert.AreEqual(1, visualInfo.Regions.Count());
            Assert.AreEqual(0, visualInfo.Regions[0].Items.Count);

            Assert.AreEqual("R", logicalInfo.Regions[0].RegionName);
            Assert.AreEqual(null, logicalInfo.Regions[0].SelectedViewModelKey);
            Assert.AreEqual(0, logicalInfo.Regions[0].Items.Count);
        }

        [Test, Retry(4)]
        public void SerializeState() {
            string logicalState = null;
            string visualState = null;
            LogicalInfo logicalInfo = null;
            VisualInfo visualInfo = null;

            Manager.Register("R", new Module("1", () => new VMTest()));
            Manager.Register("R", new Module("2", () => new VMTest() { Value = "Test" }, ViewLocator.Default.GetViewTypeName(typeof(View1_BaseTests))));
            Manager.Register("R", new Module("3", () => ViewModelSource.Create(() => new VMTest()), typeof(View1_BaseTests)));
            Manager.Register("R", new Module("4", () => ViewModelSource.Create(() => new VMTest() { Value = "Test" })));
            Manager.Inject("R", "1");
            Manager.Inject("R", "2");
            Manager.Inject("R", "3");
            Manager.Inject("R", "4");
            ContentControl c = new ContentControl();
            UIRegion.SetRegion(c, "R");
            Window.Content = c;
            Window.Show();

            Manager.Save(out logicalState, out visualState);
            logicalInfo = LogicalInfo.Deserialize(logicalState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(1, logicalInfo.Regions.Count());
            Assert.AreEqual(0, visualInfo.Regions[0].Items.Count());
            Assert.AreEqual("1", logicalInfo.Regions[0].SelectedViewModelKey);
            Func<int, RegionItemInfo> getItem = x => logicalInfo.Regions[0].Items[x];

            Assert.AreEqual("1", getItem(0).Key);
            Assert.AreEqual(null, getItem(0).ViewName);
            Assert.AreEqual(typeof(VMTest).FullName, getItem(0).ViewModelName);
            Assert.AreEqual(typeof(VMTest).AssemblyQualifiedName, getItem(0).ViewModelStateType);
            var vmState = (VMTest)StateSerializer.Default.DeserializeState(getItem(0).ViewModelState.State, Type.GetType(getItem(0).ViewModelStateType));
            Assert.AreEqual(null, vmState.Value);

            Assert.AreEqual("2", getItem(1).Key);
            Assert.AreEqual(typeof(View1_BaseTests).FullName, getItem(1).ViewName);
            Assert.AreEqual(typeof(VMTest).FullName, getItem(1).ViewModelName);
            Assert.AreEqual(typeof(VMTest).AssemblyQualifiedName, getItem(1).ViewModelStateType);
            vmState = (VMTest)StateSerializer.Default.DeserializeState(getItem(1).ViewModelState.State, Type.GetType(getItem(1).ViewModelStateType));
            Assert.AreEqual("Test", vmState.Value);

            Assert.AreEqual("3", getItem(2).Key);
            Assert.AreEqual(typeof(View1_BaseTests).FullName, getItem(2).ViewName);
            Assert.AreEqual("IsPOCOViewModel=True;" + typeof(VMTest).FullName, getItem(2).ViewModelName);
            Assert.AreNotEqual(typeof(VMTest).AssemblyQualifiedName, getItem(2).ViewModelStateType);
            Assert.AreEqual(ViewModelSource.Create(() => new VMTest()).GetType().AssemblyQualifiedName, getItem(2).ViewModelStateType);
            vmState = (VMTest)StateSerializer.Default.DeserializeState(getItem(2).ViewModelState.State, ViewModelSource.Create(() => new VMTest()).GetType());
            Assert.AreEqual(null, vmState.Value);

            Assert.AreEqual("4", getItem(3).Key);
            Assert.AreEqual(null, getItem(3).ViewName);
            Assert.AreEqual("IsPOCOViewModel=True;" + typeof(VMTest).FullName, getItem(3).ViewModelName);
            Assert.AreNotEqual(typeof(VMTest).AssemblyQualifiedName, getItem(3).ViewModelStateType);
            Assert.AreEqual(ViewModelSource.Create(() => new VMTest()).GetType().AssemblyQualifiedName, getItem(3).ViewModelStateType);
            vmState = (VMTest)StateSerializer.Default.DeserializeState(getItem(3).ViewModelState.State, ViewModelSource.Create(() => new VMTest()).GetType());
            Assert.AreEqual("Test", vmState.Value);
        }
        [Test]
        public void DeserializeState() {
            string logicalState = null;
            string visualState = null;

            VMTest vm = null;
            Manager.Register("R", new Module("1", () => vm = new VMTest() { Value = "Test" }));
            Manager.Inject("R", "1");
            ContentControl c = new ContentControl();
            UIRegion.SetRegion(c, "R");

            Manager.Save(out logicalState, out visualState);
            Manager.Restore(logicalState, visualState);
            Assert.AreNotEqual(vm, Manager.GetRegion("R").GetViewModel("1"));
            vm = (VMTest)Manager.GetRegion("R").GetViewModel("1");
            Assert.AreEqual("Test", vm.Value);
        }
        [Test]
        public void DeserializeEvents() {
            string logicalState = null;
            string visualState = null;

            Manager.Register("R", new Module("1", () => new VMTest()));
            Manager.Register("R", new Module("2", () => new VMTest()));
            ContentControl c = new ContentControl();
            UIRegion.SetRegion(c, "R");
            Window.Content = c;
            Window.Show();
            var serviceHelper = InjectionTestHelper.CreateServiceHelper(c, "R");

            Manager.Inject("R", "1");
            Manager.Inject("R", "2");
            serviceHelper.AssertSelectionChanged(1);
            Assert.AreEqual("1", Manager.GetRegion("R").SelectedKey);

            Manager.Save(out logicalState, out visualState);
            Manager.Navigate("R", "2");
            Assert.AreEqual("2", Manager.GetRegion("R").SelectedKey);
            serviceHelper.AssertSelectionChanged(2);

            Manager.Restore(logicalState, visualState);
            Assert.AreEqual("1", Manager.GetRegion("R").SelectedKey);
            serviceHelper.AssertSelectionChanged(4);
            serviceHelper.AssertViewModelRemoving(0);
            serviceHelper.AssertViewModelRemoved(2);
        }
        [Test]
        public void DisableEnable() {
            ContentControl c1 = new ContentControl();
            ContentControl c2 = new ContentControl();
            UIRegion.SetRegion(c1, "R1");
            UIRegion.SetRegion(c2, "R2");
            Manager.Register("R1", new Module("1", () => new VMTest()));
            Manager.Register("R1", new Module("2", () => new VMTest()));
            Manager.Register("R2", new Module("1", () => new VMTest()));
            Manager.Register("R2", new Module("2", () => new VMTest()));
            Manager.Inject("R1", "1");
            Manager.Inject("R1", "2");
            Manager.Inject("R2", "1");
            Manager.Inject("R2", "2");

            string logicalState = null;
            string visualState = null;
            Manager.Save(out logicalState, out visualState);
            var logicalInfo = LogicalInfo.Deserialize(logicalState);
            var visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(2, logicalInfo.Regions.Count());
            Assert.AreEqual("R1", logicalInfo.Regions[0].RegionName);
            Assert.AreEqual("R2", logicalInfo.Regions[1].RegionName);
            Assert.AreEqual(2, logicalInfo.Regions[0].Items.Count());
            Assert.AreEqual(2, logicalInfo.Regions[1].Items.Count());

            Manager.GetRegion("R1").LogicalSerializationMode = LogicalSerializationMode.Disabled;
            Manager.Save(out logicalState, out visualState);
            logicalInfo = LogicalInfo.Deserialize(logicalState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(2, logicalInfo.Regions.Count());
            Assert.AreEqual("R1", logicalInfo.Regions[0].RegionName);
            Assert.AreEqual("R2", logicalInfo.Regions[1].RegionName);
            Assert.AreEqual(0, logicalInfo.Regions[0].Items.Count());
            Assert.AreEqual(2, logicalInfo.Regions[1].Items.Count());

            Manager.GetRegion("R1").LogicalSerializationMode = LogicalSerializationMode.Enabled;
            Manager.Save(out logicalState, out visualState);
            logicalInfo = LogicalInfo.Deserialize(logicalState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(2, logicalInfo.Regions[0].Items.Count());
            Assert.AreEqual(2, logicalInfo.Regions[1].Items.Count());

            Manager.GetRegion("R1").SetLogicalSerializationMode("1", LogicalSerializationMode.Disabled);
            Manager.Save(out logicalState, out visualState);
            logicalInfo = LogicalInfo.Deserialize(logicalState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(1, logicalInfo.Regions[0].Items.Count());
            Assert.AreEqual("2", logicalInfo.Regions[0].Items[0].Key);
            Assert.AreEqual(2, logicalInfo.Regions[1].Items.Count());

            Manager.GetRegion("R1").LogicalSerializationMode = LogicalSerializationMode.Disabled;
            Manager.GetRegion("R1").SetLogicalSerializationMode("2", LogicalSerializationMode.Enabled);
            Manager.Save(out logicalState, out visualState);
            logicalInfo = LogicalInfo.Deserialize(logicalState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(1, logicalInfo.Regions[0].Items.Count());
            Assert.AreEqual("2", logicalInfo.Regions[0].Items[0].Key);
            Assert.AreEqual(2, logicalInfo.Regions[1].Items.Count());

            Manager.GetRegion("R1").SetLogicalSerializationMode("1", null);
            Manager.GetRegion("R1").SetLogicalSerializationMode("2", null);
            Manager.Save(out logicalState, out visualState);
            logicalInfo = LogicalInfo.Deserialize(logicalState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(0, logicalInfo.Regions[0].Items.Count());
            Assert.AreEqual(2, logicalInfo.Regions[1].Items.Count());
        }
    }
#if !FREE
    public class VisualSerializationTests_V : StackPanel {
        public static bool IsEnabledSerialization = true;
        public static bool AutoSave = true;
        public static bool AutoRestore = true;

        public SerializableControl Control { get; private set; }
        public VisualSerializationTests_V() {
            Control = new SerializableControl();
            Children.Add(Control);
            if(IsEnabledSerialization) {
                Interactivity.Interaction.GetBehaviors(this).Add(
                    new VisualStateService() { AutoSaveState = AutoSave, AutoRestoreState = AutoRestore });
            }
        }
    }
    public class SerializableControl : TextBox {
        [XtraSerializableProperty]
        public string Value { get { return Text; } set { Text = value; } }

        static SerializableControl() {
            DXSerializer.SerializationIDDefaultProperty.OverrideMetadata(typeof(SerializableControl), new UIPropertyMetadata(typeof(SerializableControl).Name));
            DXSerializer.SerializationProviderProperty.OverrideMetadata(typeof(SerializableControl), new UIPropertyMetadata(new SerializationProvider()));
        }
        public SerializableControl() {
            Value = "12345";
        }
    }
    [TestFixture]
    public class VisualSerializationTests : BaseWpfFixture {
        public class VM { }

        public IModuleManager Manager { get { return ModuleManager.DefaultManager; } }
        public IModuleWindowManager WindowManager { get { return ModuleManager.DefaultWindowManager; } }
        protected override void SetUpCore() {
            base.SetUpCore();
            InjectionTestHelper.SetUp(typeof(VisualSerializationTests).Assembly);
            VisualSerializationTests_V.AutoSave = true;
            VisualSerializationTests_V.AutoRestore = true;
        }
        protected override void TearDownCore() {
            InjectionTestHelper.TearDown();
            base.TearDownCore();
        }

        [Test]
        public void NoSerializationService() {
            VisualSerializationTests_V.IsEnabledSerialization = false;
            Grid container = new Grid();
            UIRegion.SetRegion(container, "R");
            Window.Content = container;

            Manager.Register("R", new Module("1", () => ViewModelSource.Create(() => new VM()), typeof(VisualSerializationTests_V)));
            Manager.Inject("R", "1");
            Window.Show();

            string logicalState = null;
            string visualState = null;
            VisualInfo visualInfo = null;
            Manager.Save(out logicalState, out visualState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(0, visualInfo.Regions[0].Items.Count);
        }
        [Test]
        public void DefaultLayout() {
            Grid container = new Grid();
            UIRegion.SetRegion(container, "R");
            Window.Content = container;

            Manager.Register("R", new Module("1", () => ViewModelSource.Create(() => new VM()), typeof(VisualSerializationTests_V)));
            Manager.Inject("R", "1");
            Window.Show();

            var vm = Manager.GetRegion("R").GetViewModel("1");
            var serv = vm.GetService<IVisualStateService>();
            Assert.AreEqual(true, serv.DefaultState.Contains("12345"));

            string logicalState = null;
            string visualState = null;
            VisualInfo visualInfo = null;
            Manager.Save(out logicalState, out visualState);
            visualInfo = VisualInfo.Deserialize(visualState);
            Assert.AreEqual(
                serv.DefaultState.Replace("\n", "").Replace("\r", ""), 
                visualInfo.Regions[0].Items[0].State.State.Replace("\n", "").Replace("\r", ""));
        }
        [Test]
        public void AutoSaveAndAutoRestore() {
            Grid container = new Grid();
            UIRegion.SetRegion(container, "R");
            Window.Content = container;

            Manager.Register("R", new Module("1", () => ViewModelSource.Create(() => new VM()), typeof(VisualSerializationTests_V)));
            Manager.Inject("R", "1");
            Window.Show();
            
            var vm = Manager.GetRegion("R").GetViewModel("1");
            VisualSerializationTests_V v = LayoutTreeHelper.GetVisualChildren(container).OfType<VisualSerializationTests_V>().First(x => x.DataContext == vm);
            Assert.AreEqual("12345", v.Control.Value);
            v.Control.Value = "abcd";

            Manager.Remove("R", "1");
            Manager.Inject("R", "1");
            DispatcherHelper.DoEvents();

            vm = Manager.GetRegion("R").GetViewModel("1");
            v = LayoutTreeHelper.GetVisualChildren(container).OfType<VisualSerializationTests_V>().First(x => x.DataContext == vm);
            Assert.AreEqual("abcd", v.Control.Value);
        }

        void GetServices(string region, string key, DependencyObject container, out object vm, out VisualSerializationTests_V v, out VisualStateService serv) {
            vm = Manager.GetRegion(region).GetViewModel(key);
            serv = (VisualStateService)vm.GetService<IVisualStateService>();
            var vmm = vm;
            v = LayoutTreeHelper.GetVisualChildren(container).OfType<VisualSerializationTests_V>().First(x => x.DataContext == vmm);
        }
        [Test]
        public void DisableAutoSaveAndAutoRestore() {
            Grid container = new Grid();
            UIRegion.SetRegion(container, "R");
            Window.Content = container;

            Manager.Register("R", new Module("1", () => ViewModelSource.Create(() => new VM()), typeof(VisualSerializationTests_V)));
            Manager.Inject("R", "1");
            Window.Show();
            object vm;
            VisualSerializationTests_V v;
            VisualStateService serv;

            GetServices("R", "1", container, out vm, out v, out serv);
            Assert.AreEqual("12345", v.Control.Value);
            v.Control.Value = "abcd";
            serv.AutoSaveState = false;
            VisualSerializationTests_V.AutoSave = false;

            Manager.Remove("R", "1");
            Manager.Inject("R", "1");
            DispatcherHelper.DoEvents();
            GetServices("R", "1", container, out vm, out v, out serv);
            Assert.AreEqual("12345", v.Control.Value);

            v.Control.Value = "abcd";
            serv.AutoSaveState = true;
            serv.AutoRestoreState = false;
            VisualSerializationTests_V.AutoSave = true;
            VisualSerializationTests_V.AutoRestore = false;
            Manager.Remove("R", "1");
            Manager.Inject("R", "1");
            DispatcherHelper.DoEvents();
            GetServices("R", "1", container, out vm, out v, out serv);

            Assert.AreEqual("12345", v.Control.Value);
            serv.RestoreState();
            DispatcherHelper.DoEvents();
            Assert.AreEqual("abcd", v.Control.Value);
        }
    }
#endif
}
