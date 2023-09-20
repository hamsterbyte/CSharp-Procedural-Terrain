using Godot;
using Godot.Collections;

public static class Extensions{

    public static float Wrap(this float value, float min, float max){
        if (value > max){
            value = value % max;
        }

        if (value < min){
            value = max - value % max;
        }

        return value;
    }
    
}