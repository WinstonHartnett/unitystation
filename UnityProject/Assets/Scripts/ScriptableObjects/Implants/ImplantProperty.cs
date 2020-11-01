﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public abstract class ImplantProperty : ScriptableObject
	{
		public abstract void ImplantUpdate(ImplantBase implant, LivingHealthMasterBase healthMaster);
	}

}