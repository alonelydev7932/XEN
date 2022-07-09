# XEN LOADER
A simple console menu-based UI loader to run unmanaged libraries. Perfect for those times when you need a secure way to load unprotected executable with out source.

<p align="center">
  <img  src="Preview.gif">
</p>

# Usage
It's pretty simple to use. Here's an example below of some of the code:

```java
    // Using keyauth download method we download our exe file to memory.
    
    byte[] result = KeyAuthApp.download("410684");
    
    // Once the file has been downloaded we then run it through our application.
    Run(result);
    
    // Make sure you fix buffer value for the line set ( "0x382" )
    
    Buffer.SetByte(buffer, e_lfanew + "0x382", 2);
    
    // You should change this depending on the exe you are trying to run.
}
```
# Note 
* Using protected executable files may break this method.
* The example was a simple winform application unprotected 382 bytes.
