using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarPool : GenericComponentPool<CarController>
{
    protected override void InitializeObject(CarController component)
    {

    }

    protected override void ReuseObject(CarController component)
    {
        // Reset the car
        component.Reset();
    }
}
