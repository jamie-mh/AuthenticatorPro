using System;
using Android.App;
using Android.Runtime;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace ProAuth.Utilities
{
    internal class CustomActionBarDrawerToggle : ActionBarDrawerToggle
    {
        public Action IdleAction { get; set; }

        protected CustomActionBarDrawerToggle(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CustomActionBarDrawerToggle(Activity activity, DrawerLayout drawerLayout, Toolbar toolbar, int openDrawerContentDescRes, int closeDrawerContentDescRes) : base(activity, drawerLayout, toolbar, openDrawerContentDescRes, closeDrawerContentDescRes)
        {
        }

        public CustomActionBarDrawerToggle(Activity activity, DrawerLayout drawerLayout, int openDrawerContentDescRes, int closeDrawerContentDescRes) : base(activity, drawerLayout, openDrawerContentDescRes, closeDrawerContentDescRes)
        {
        }

        public override void OnDrawerStateChanged(int newState)
        {
            base.OnDrawerStateChanged(newState);

            if(IdleAction != null && newState == DrawerLayout.StateIdle)
            {
                IdleAction.Invoke();
                IdleAction = null;
            }
        }
    }
}