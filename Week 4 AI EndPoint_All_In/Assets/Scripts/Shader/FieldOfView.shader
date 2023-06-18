Shader "FieldOfView" {
	Properties{  
		// Properties of the material
		_MainTex("Base (RGB)", 2D) = "white" {}
		_FOVColor("Field Of View Color", Color) = (1, 1, 1)
		_MainColor("MainColor", Color) = (1, 1, 1)
		_Position1("Position1",  Vector) = (0,0,0)
		_Direction1("Direction1",  Vector) = (0,0,0)
		_Position2("Position2",  Vector) = (0,0,0)
		_Direction2("Direction2",  Vector) = (0,0,0)
		_Position3("Position3",  Vector) = (0,0,0)
		_Direction3("Direction3",  Vector) = (0,0,0)
		_Position4("Position4",  Vector) = (0,0,0)
		_Direction4("Direction4",  Vector) = (0,0,0)
	}
		SubShader{
		Tags{ "RenderType" = "Diffuse" }
		// https://docs.unity3d.com/Manual/SL-SurfaceShaders.html
		CGPROGRAM
#pragma surface surf Lambert

	sampler2D _MainTex;
	//https://docs.unity3d.com/Manual/SL-DataTypesAndPrecision.html
	fixed3 _FOVColor; //Precision
	fixed3 _MainColor;
	float3 _Position1;
	float3 _Direction1;
	float3 _Position2;
	float3 _Direction2;
	float3 _Position3;
	float3 _Direction3;
	float3 _Position4;
	float3 _Direction4;

	// Values that interpolated from vertex data.
	struct Input {
		float2 uv_MainTex;
		float3 worldPos;
	};

	// Barycentric coordinates
	// http://mathworld.wolfram.com/BarycentricCoordinates.html
	bool isPointInTriangle(float2 p1, float2 p2, float2 p3, float2 pointInQuestion)
	{
		float denominator = ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));
		float a = ((p2.y -p3.y) * (pointInQuestion.x - p3.x) + (p3.x - p2.x) * (pointInQuestion.y - p3.y)) / denominator;
		float b  = ((p3.y - p1.y) * (pointInQuestion.x - p3.x) + (p1.x - p3.x) * (pointInQuestion.y - p3.y)) / denominator;
		float c  = 1 - a - b;

		return 0 <= a && a <= 1 && 0 <= b && b <= 1 && 0 <= c && c <= 1;
	}

	bool isPointInTheCircle(float2 circleCenterPoint, float2 thisPoint, float radius)
	{
		return distance(circleCenterPoint, thisPoint) <= radius;
	}

	bool isPointInTheCone(float2 circleCenterPoint, float2 thisPoint, float radius, float angle, int index)
	{
		float2 cameraDirection;

		if (index == 0)
		{
			cameraDirection = _Direction1.xz;
		}
		else if (index == 1)
		{
			cameraDirection = _Direction2.xz;
		}
		else if (index == 2)
		{
			cameraDirection = _Direction3.xz;
		}
		else if (index == 3)
		{
			cameraDirection = _Direction4.xz;
		}

		float2 dir1 = thisPoint - circleCenterPoint;
		float Dot = dot(normalize(cameraDirection), normalize(dir1));
		float angleMade = acos(Dot);
		return angleMade < angle && distance(circleCenterPoint, thisPoint) <= radius;
	}
	
	void surf(Input IN, inout SurfaceOutput o) {
		

		half4 c = tex2D(_MainTex, IN.uv_MainTex);


		float3 basePoint = _Position1.xyz;
		basePoint.y = 0.01;

		float3 basePoint1 = _Position2.xyz;
		basePoint1.y = 0.01;

		float3 basePoint2 = _Position3.xyz;
		basePoint2.y = 0.01;

		float3 basePoint3 = _Position4.xyz;
		basePoint3.y = 0.01;
		
		float offsetAngle = 45.0;
		float offsetAngleInRadians = offsetAngle * (3.14 / 180);
		
		float3 pointInQuestion = IN.worldPos;
		pointInQuestion.y = 0.01;

		c.rgb *= _MainColor;

		if (isPointInTheCone(basePoint.xz, pointInQuestion.xz, 8.1, offsetAngleInRadians, 0) || isPointInTheCone(basePoint1.xz, pointInQuestion.xz, 8.1, offsetAngleInRadians, 1) || isPointInTheCone(basePoint2.xz, pointInQuestion.xz, 8.1, offsetAngleInRadians, 2) || isPointInTheCone(basePoint3.xz, pointInQuestion.xz, 8.1, offsetAngleInRadians, 3))
		{
			o.Albedo = c.rgb * _FOVColor;
		}
		else
		{
			o.Albedo = c.rgb;
		}

	
		o.Alpha = c.a;
	}
	ENDCG
	}
		FallBack "Diffuse" //If we cannot use the subshader on specific hardware we will fallback to Diffuse shader
}
