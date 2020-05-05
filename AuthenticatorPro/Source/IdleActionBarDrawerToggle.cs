using System;
using Android.App;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.DrawerLayout.Widget;

namespace AuthenticatorPro
{
    internal class IdleActionBarDrawerToggle : ActionBarDrawerToggle
    {
        public IdleActionBarDrawerToggle(Activity activity, DrawerLayout drawerLayout, Toolbar toolbar,
            int openDrawerContentDescRes, int closeDrawerContentDescRes) : base(activity, drawerLayout, toolbar,
            openDrawerContentDescRes, closeDrawerContentDescRes)
        {

        }

        public Action IdleAction { get; set; }

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