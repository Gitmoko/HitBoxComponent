using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
[Serializable]
public class HitBoxes : ScriptableObject
{
    [SerializeField]
    public List<HitBoxesKeyVal> data = new List<HitBoxesKeyVal>();

    private bool sorted = false;

    public HitBoxKeyFrames this[string key] {
        get {
            if (!sorted)
            {
                data.Sort();
                sorted = true;
            }
            int i = data.BinarySearch(new HitBoxesKeyVal(key, null));
            if (i < 0)
            {
                var adddata = new HitBoxKeyFrames();
                data.Add(new HitBoxesKeyVal(key, adddata));
                data.Sort();
                return adddata;

            }
            return data[i].Value;

        }
        set {
            if (!sorted)
            {
                data.Sort();
                sorted = true;
            }
            int i = data.BinarySearch(new HitBoxesKeyVal(key, null));
            if (i < 0)
            {
                data.Add(new HitBoxesKeyVal(key, new HitBoxKeyFrames()));

            }
            else
            {
                data[i].Value = value;
            }
            data.Sort();
        }
    }

}
