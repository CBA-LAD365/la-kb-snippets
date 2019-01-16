package com.cafex.liveassist_android_demo;

import android.content.DialogInterface;
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import android.os.Bundle;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import com.cafex.liveassist.LiveAssistChatStyle;
import com.cafex.liveassist.LiveAssistConfig;
import com.cafex.liveassist.LiveAssistView;
//{Auth Chat} Addition imports for Authenticated chat
import com.cafex.liveassist.LiveAssistAuth;
import com.cafex.liveassist.LiveAssistDelegate;


//{Auth Chat} Implement the LiveAssistDelegate interface
public class MainActivity extends AppCompatActivity implements LiveAssistDelegate{

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        onCreateLiveAssist();
        onCreateWebView();
        WebView.setWebContentsDebuggingEnabled(true);
    }

    private void onCreateLiveAssist(){

        String[] sections = {""};
        LiveAssistConfig liveAssistConfig = new LiveAssistConfig("PutAccountNumberHere",sections,LiveAssistChatStyle.AUTO);

        //{Auth Chat} Set the method name and LiveAssistDelegate on your LiveAssistConfig Object before you initialize the LiveAssistView
        liveAssistConfig.setJavascriptMethodName("authoriseChatWithCallback");
        liveAssistConfig.setLiveAssistDelegate(this);

        LiveAssistView liveAssistView = (LiveAssistView) findViewById(R.id.live_assist);
        liveAssistView.loadWithConfig(liveAssistConfig);
    }

    private void showNoAccountIdentifierAlert(){
        AlertDialog alertDialog = new AlertDialog.Builder(MainActivity.this).create();
        alertDialog.setTitle(getString(R.string.dialog_header));
        alertDialog.setMessage(getString(R.string.no_account_id_message));
        alertDialog.setButton(AlertDialog.BUTTON_NEUTRAL, getString(R.string.ok_button),
                new DialogInterface.OnClickListener() {
                    public void onClick(DialogInterface dialog, int which) {
                        dialog.dismiss();
                    }
                });
        alertDialog.show();
    }

    private void onCreateWebView(){
        WebView webView = (WebView) findViewById(R.id.webview);
        webView.setWebViewClient(new WebViewClient());
        WebSettings webSettings = webView.getSettings();
        webSettings.setJavaScriptEnabled(true);
        webView.loadUrl(getString(R.string.weburl));

    }

    //{Auth Chat} Implement the authoriseChatWithCallback method and pass your JWT token to the authorise method.
    @Override
    public void authoriseChatWithCallback(LiveAssistAuth liveAssistAuth) {
        String jwt = "PutJWTHere";
        liveAssistAuth.authorise(jwt);
    }
}


