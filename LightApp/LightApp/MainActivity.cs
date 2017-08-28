using Android.App;
using Android.Widget;
using Android.OS;
using Android.Bluetooth;
using Android.Runtime;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace LightApp
{
    [Activity(Label = "LightApp", MainLauncher = true)]
    public class MainActivity : Activity
    {
        Button lightSwitch;
        TextView nameTV;
        TextView textView2;
        BluetoothDevice bleDevice;
        BluetoothGatt bleGatt;
        BluetoothGattCharacteristic bleLightCharacteristic;
        IList<BluetoothGattCharacteristic> bleLightCharacteristicsList;
        IList<BluetoothGattService> bleServicesList;
        GattClientObserver observer = new GattClientObserver();
        BluetoothAdapter adapter = BluetoothAdapter.DefaultAdapter;
        private readonly byte[] MAC_ADDRESS = { 0x50, 0xF1, 0x4A, 0x50, 0x9A, 0x7E };
        private const string LIGHT_SERVICE = "0000FFE0-0000-1000-8000-00805F9B34FB";
        private const string LIGHT_CHARACTERISTIC = "0000FFE1-0000-1000-8000-00805F9B34FB";

        public BluetoothGattCallback GattCallback { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            lightSwitch = FindViewById<Button>(Resource.Id.lightSwitchBtn);
            nameTV = FindViewById<TextView>(Resource.Id.textView1);
            textView2 = FindViewById<TextView>(Resource.Id.textView2);

            ConnectToArduino();

            lightSwitch.Click += delegate
            {
                bleLightCharacteristic.SetValue("1");
                bleGatt.WriteCharacteristic(bleLightCharacteristic);
            };
        }

        private async void ConnectToArduino()
        {
            bleDevice = adapter.GetRemoteDevice(MAC_ADDRESS);                       // find the device
            bleGatt = bleDevice.ConnectGatt(Application.Context, true, observer);   // connect to the device
            await Task.Delay(10000);                                                // wait till it is connected

            nameTV.Text = bleGatt.Device.Name;
            bleGatt.DiscoverServices();                                             // discover services of the device
            bleServicesList = bleGatt.Services;                                     // save the services

            // if services were discovered, search for the service to switch the light and save the right characteristic
            if (bleServicesList.Count > 0)
            {
                foreach (BluetoothGattService service in bleServicesList)
                {
                    if (service.Uuid.ToString().ToUpper().Equals(LIGHT_SERVICE))
                    {
                        bleLightCharacteristicsList = service.Characteristics;
                        break;
                    }
                }

                foreach (BluetoothGattCharacteristic characteristic in bleLightCharacteristicsList)
                {
                    if (characteristic.Uuid.ToString().ToUpper().Equals(LIGHT_CHARACTERISTIC))
                    {
                        bleLightCharacteristic = characteristic;
                        break;
                    }
                }
            }
            else
            {
                textView2.Text = "bleService failed.";
            }
        }
    }
    
    public class GattClientObserver : BluetoothGattCallback
    {
        private void OnConnectionStateChanged(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            switch (newState)
            {
                case ProfileState.Connected:
                    break;
                case ProfileState.Disconnected:
                    break;
                case ProfileState.Connecting:
                    break;
                case ProfileState.Disconnecting:
                    break;
            }
        }
    }
}

