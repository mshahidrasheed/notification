# Notification
Notification is a windows form application that used for generate popup alert messages or notification.
User of this application edit alert form according his/her requirements. 
This is a custom alert windows form that having feature of auto hide.

# Step 1:
* Clone this project and load into Visual Studio
* Build this project
* Or find already Build Notification.dll file in Build folder

# Step 2:
* Add Notification.dll file in your current project References where you want to show notification alert messages.
* Add namespace of Notification in your Class

**using Notification;**

* Call Show() function using Class name because Show is static function.
**public static short Show(string desc, Type type = Type.INFO, string tit = "Notifier", bool isDialog = false, int timeout = 0);**
        
**For Example:**

            **string message = "My first message Notification";
            Notification.Type type = Notification.Type.INFO;
            string title = "Notification";

            Notification.Notification.Show(message, type, title, false, 3000);**


# Notification Form Types:

There are four type of Notification form
* INFO
* OK
* ERROR
* WARNING

Every type represent a diffrent message type. Every type having a diffrent colors and icons for identifications of message.
