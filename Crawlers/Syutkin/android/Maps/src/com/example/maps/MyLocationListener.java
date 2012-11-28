package com.example.maps;

import android.app.Activity;
import android.app.AlertDialog;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Bundle;

public class MyLocationListener extends Activity implements LocationListener {
	private LocationManager myManager;
	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);		
		setContentView(R.layout.activity_main);
		myManager = (LocationManager) getSystemService(LOCATION_SERVICE);		
		myManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 0, 0, this);
		
        AlertDialog.Builder builder = new AlertDialog.Builder(this);

        // 2. Chain together various setter methods to set the dialog characteristics
        builder.setMessage("wertyu");               

        // 3. Get the AlertDialog from create()
        AlertDialog dialog = builder.create();        
        dialog.show();
	}

	public void onLocationChanged(Location location) {

	}

	public void onProviderDisabled(String provider) {
	}

	public void onProviderEnabled(String provider) {
	}

	public void onStatusChanged(String provider, int status, Bundle extras) {
	}
}