/*
This file is part of Keepass2Android, Copyright 2013 Philipp Crocoll. This file is based on Keepassdroid, Copyright Brian Pellin.

  Keepass2Android is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  Keepass2Android is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with Keepass2Android.  If not, see <http://www.gnu.org/licenses/>.
  */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using KeePassLib;
using Android.Util;
using KeePassLib.Utility;
using keepass2android.view;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Support.V7.App;
using KeePassLib.Security;
using AlertDialog = Android.App.AlertDialog;
using Object = Java.Lang.Object;

namespace keepass2android
{
	[Activity(Label = "@string/app_name", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden, Theme = "@style/MyTheme_ActionBar")]
	[MetaData("android.app.default_searchable", Value = "keepass2android.search.SearchResults")]
#if NoNet
    [MetaData("android.app.searchable", Resource = "@xml/searchable_offline")]
#else
#if DEBUG
    [MetaData("android.app.searchable", Resource = "@xml/searchable_debug")]
#else
    [MetaData("android.app.searchable", Resource = "@xml/searchable")]
#endif
#endif
    [IntentFilter(new string[]{"android.intent.action.SEARCH"})]
	[MetaData("android.app.searchable",Resource=AppNames.Searchable)]
	public class GroupActivity : GroupBaseActivity {
		
		public const int Uninit = -1;
		
		
		private const String Tag = "Group Activity:";
		
		public static void Launch(Activity act, AppTask appTask) {
			Launch(act, null, appTask);
		}
		
		public static void Launch (Activity act, PwGroup g, AppTask appTask)
		{
			// Need to use PwDatabase since group may be null
            PwDatabase db = App.Kp2a.GetDb().KpDatabase;

			if (db == null) {
				// Reached if db is null
				Log.Debug (Tag, "Tried to launch with null db");
				return;
			}

			Intent i = new Intent(act, typeof(GroupActivity));
				
			if ( g != null ) {
				i.PutExtra(KeyEntry, g.Uuid.ToHexString());
			}
			appTask.ToIntent(i);

			act.StartActivityForResult(i,0);
		}

		protected PwUuid RetrieveGroupId(Intent i)
		{
			String uuid = i.GetStringExtra(KeyEntry);
			
			if ( String.IsNullOrEmpty(uuid) ) {
				return null;
			}
			return new PwUuid(MemUtil.HexStringToByteArray(uuid));
		}

		public override void SetupNormalButtons()
		{
		    SetNormalButtonVisibility(AddGroupEnabled, AddEntryEnabled);
		}

        protected override bool AddEntryEnabled
		{
			get { return App.Kp2a.GetDb().CanWrite && ((this.Group.ParentGroup != null) || App.Kp2a.GetDb().DatabaseFormat.CanHaveEntriesInRootGroup); }
		}

		private class TemplateListAdapter : ArrayAdapter<PwEntry>
		{
			public TemplateListAdapter(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
			{
			}

			public TemplateListAdapter(Context context, int textViewResourceId) : base(context, textViewResourceId)
			{
			}

			public TemplateListAdapter(Context context, int resource, int textViewResourceId) : base(context, resource, textViewResourceId)
			{
			}

			public TemplateListAdapter(Context context, int textViewResourceId, PwEntry[] objects) : base(context, textViewResourceId, objects)
			{
			}

			public TemplateListAdapter(Context context, int resource, int textViewResourceId, PwEntry[] objects) : base(context, resource, textViewResourceId, objects)
			{
			}

			public TemplateListAdapter(Context context, int textViewResourceId, IList<PwEntry> objects) : base(context, textViewResourceId, objects)
			{
			}

			public TemplateListAdapter(Context context, int resource, int textViewResourceId, IList<PwEntry> objects) : base(context, resource, textViewResourceId, objects)
			{
			}

			public override View GetView(int position, View convertView, ViewGroup parent)
			{
				View v = base.GetView(position, convertView, parent);

				TextView tv = (TextView)v.FindViewById(Android.Resource.Id.Text1);
				tv.SetPadding(tv.PaddingLeft,0,tv.PaddingRight,0);

				PwEntry templateEntry = this.GetItem(position);

				var bmp =
					Bitmap.CreateScaledBitmap(
						Util.DrawableToBitmap(App.Kp2a.GetDb()
							.DrawableFactory.GetIconDrawable(Context, App.Kp2a.GetDb().KpDatabase, templateEntry.IconId, PwUuid.Zero, false)),
						(int)Util.convertDpToPixel(80, Context),
						(int)Util.convertDpToPixel(80, Context),
						true);
				
				
				Drawable icon = new BitmapDrawable(bmp);

				if (
						PreferenceManager.GetDefaultSharedPreferences(Context)
							.GetString("IconSetKey", Context.PackageName) == Context.PackageName)
				{
					Android.Graphics.PorterDuff.Mode mMode = Android.Graphics.PorterDuff.Mode.SrcAtop;
					Color color = new Color(189, 189, 189);
					icon.SetColorFilter(color, mMode);
				}
				
				//Put the image on the TextView
				tv.SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);
				tv.Text = templateEntry.Strings.ReadSafe(PwDefs.TitleField);
				tv.SetTextSize(ComplexUnitType.Dip, 20);
				
				tv.CompoundDrawablePadding = (int)Util.convertDpToPixel(8, Context);

				return v;
			}
		};

	    protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			
			
			if (IsFinishing) {
				return;
			}
			
			SetResult (KeePass.ExitNormal);
			
			Log.Warn (Tag, "Creating group view");
			Intent intent = Intent;
			
			PwUuid id = RetrieveGroupId (intent);
			
			Database db = App.Kp2a.GetDb();
			if (id == null) {
				Group = db.Root;
			} else {
				Group = db.Groups[id];
			}
			
			Log.Warn (Tag, "Retrieved group");
			if (Group == null) {
				Log.Warn (Tag, "Group was null");
				return;
			}
			
			
			if (AddGroupEnabled) {
				// Add Group button
				View addGroup = FindViewById (Resource.Id.fabAddNewGroup);
				addGroup.Click += (sender, e) => {
					GroupEditActivity.Launch (this, Group);
				};
			}
			
			
            if (AddEntryEnabled) 
            {
				View addEntry = FindViewById (Resource.Id.fabAddNewEntry);
				addEntry.Click += (sender, e) =>
				{
					PwEntry defaultTemplate = new PwEntry(false, false);
					defaultTemplate.IconId = PwIcon.Key;
					defaultTemplate.Strings.Set(PwDefs.TitleField, new ProtectedString(false, GetString(Resource.String.DefaultTemplate)));
					List<PwEntry> templates = new List<PwEntry>() { defaultTemplate }; 
					if (!PwUuid.Zero.Equals(App.Kp2a.GetDb().KpDatabase.EntryTemplatesGroup))
					{
						templates.AddRange(App.Kp2a.GetDb().Groups[App.Kp2a.GetDb().KpDatabase.EntryTemplatesGroup].Entries.OrderBy(entr => entr.Strings.ReadSafe(PwDefs.TitleField)));
					}

					new AlertDialog.Builder(this)
						.SetAdapter(new TemplateListAdapter(this, Android.Resource.Layout.SelectDialogItem,
							Android.Resource.Id.Text1, templates), (o, args) =>
							{

								EntryEditActivity.Launch(this, Group, templates[args.Which].Uuid, AppTask); 
							})
						.Show();


					
				};
                 
			}
			
			SetGroupTitle();
			SetGroupIcon();
			
			FragmentManager.FindFragmentById<GroupListFragment>(Resource.Id.list_fragment).ListAdapter = new PwGroupListAdapter(this, Group);
			Log.Warn(Tag, "Finished creating group");
			
		}

		public override void OnCreateContextMenu(IContextMenu menu, View v,
		                                         IContextMenuContextMenuInfo  menuInfo) {
			
			AdapterView.AdapterContextMenuInfo acmi = (AdapterView.AdapterContextMenuInfo) menuInfo;
			ClickView cv = (ClickView) acmi.TargetView;
			cv.OnCreateMenu(menu, menuInfo);
		}
		
		public override void OnBackPressed()
		{
			base.OnBackPressed();
			//if ((Group != null) && (Group.ParentGroup != null))
				//OverridePendingTransition(Resource.Animation.anim_enter_back, Resource.Animation.anim_leave_back);
		}
		
		public override bool OnContextItemSelected(IMenuItem item) {
			AdapterView.AdapterContextMenuInfo acmi = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
			ClickView cv = (ClickView) acmi.TargetView;
			
			return cv.OnContextItemSelected(item);
		}
		
	}
}

