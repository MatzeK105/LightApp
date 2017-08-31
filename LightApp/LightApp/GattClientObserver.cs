using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Bluetooth;

namespace LightApp
{
    class GattClientObserver : BluetoothGattCallback
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