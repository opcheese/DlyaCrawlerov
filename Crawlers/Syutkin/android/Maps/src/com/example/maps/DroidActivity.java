package com.example.maps;

import org.apache.cordova.DroidGap;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Bundle;
import android.app.Activity;
import android.app.AlertDialog;
import android.view.Menu;

public class DroidActivity extends DroidGap {

	private LocationManager myManager;
	private LocationListener myListener;
	
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        //myManager = (LocationManager) getSystemService(LOCATION_SERVICE);        
        //myManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 0, 0, myListener);
        super.loadUrl("file:///android_asset/www/index.html");
     // 1. Instantiate an AlertDialog.Builder with its constructor
        appView.addJavascriptInterface(this, "MyCls");    //ADD THIS
        //MyLocationListener a = new MyLocationListener();
        //a.onCreate(savedInstanceState);
        
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        getMenuInflater().inflate(R.menu.activity_main, menu);
        return true;
    }
}
