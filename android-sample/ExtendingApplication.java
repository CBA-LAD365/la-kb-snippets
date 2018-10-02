//This contains some code snippets for extending an existing Andorid application class to use Live Assist for 365
//Please find the full article on the Live Assist Knowledge Base

//Add this to you: AndroidManifest.xml
<application android:name="com.cafex.liveassist.LiveAssistApplication" Modifying an existing Application.


// 1. Import the following classes from the Live Assist library

import com.alicecallsbob.assist.sdk.core.AssistApplication;
import com.alicecallsbob.assist.sdk.core.AssistCore;
import com.alicecallsbob.assist.sdk.core.AssistCoreImpl;

// 2. Implement the AssistApplication interface

public class YouApplication extends Application implements AssistApplication 
{

// 3. Add Assistcore to your Application class

private AssistCore assistCore;

private void createAssistCore(Application application)
{
   assistCore = new AssistCoreImpl(application);
}

private void terminateAssistCore()
{
   assistCore.terminate();
   assistCore = null;
}

//4. Return Assist core using the getAssistCore method.

@Override
public AssistCore getAssistCore()
{
   return assistCore;
}

//5. Create and delete the assistCore instance in your onCreate and onTerminate methods.

@Override
public void onCreate()
{
   super.onCreate();
   // YOUR CODE...
   createAssistCore(this);
}

@Override
public void onTerminate()
{
   super.onTerminate();
   //YOUR CODE...
   terminateAssistCore();
}
