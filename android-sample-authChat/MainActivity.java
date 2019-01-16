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
        LiveAssistConfig liveAssistConfig = new LiveAssistConfig(19956324,sections,LiveAssistChatStyle.AUTO);

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
        String jwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI2MDJDRTBGMS03OTBELUU3MTEtODBFOS1DNDM0NkJBQ0ZCQkMiLCJnaXZlbl9uYW1lIjoiT3B0aW9uYWwiLCJpc3MiOiJzb3VyY2Ugc3RyaW5nIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE3MTYyMzkwMjJ9.znR7SO2Ibz4ZQR07GbSMnvHBSFI4HZ4e2xS4AjG6JBllP5VWdMzg4YZJSAlo6-UV9HvljA3Oc3cIyqHa9SVqZg6PugQeRg5bwu1HwawvikXWL2hW-QS3l9JaUsY267ZL-FEp9402U0hsB96hOKsX-ppPM1cn7Ky1jRyJbeaYvMI";
        liveAssistAuth.authorise(jwt);
    }
}


