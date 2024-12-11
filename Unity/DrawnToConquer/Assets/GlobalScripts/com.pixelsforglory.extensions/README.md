# Pixels for Glory Extensions
Extensions used by Pixels for Glory libraries 

## Installation
- Point to this repository to [install as a package in a Unity project](https://docs.unity3d.com/Manual/upm-git.html)

## Usage
- IsAlmostZero
    
    Tests that a float is within a tolerance of zero:
    
        float zero = 0.0f;
        float AlmostZero = 0.0000000000000000000000000001f;
        float notZero = 0.00001f;
      
        zero.IsAlmostZero();         // True
        AlmostZero.IsAlmostZero();   // True
        notZero.IsAlmostZero();      // False
        notZero.IsAlmostZero(0.01f); // True
    
- IsAlmostEqualTo
	
    Tests that a float is within a tolerance of another float:
	
        float one = 1.0f;
        float oneish = 1.01f;
      
        one.IsAlmostEqualTo(one);          // True
        one.IsAlmostEqualTo(oneish);       // False
        one.IsAlmostEqualTo(oneish, 0.1f); // True
      
