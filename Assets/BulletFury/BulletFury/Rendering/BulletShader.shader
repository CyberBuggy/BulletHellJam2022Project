Shader "BulletFury/Unlit Bullet" {
  //show values to edit in inspector
  Properties{
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
  }

  SubShader
  {
    Tags{ "RenderType"="Transparent" "Queue"="Transparent"}
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha
    
    Pass{
      CGPROGRAM
      //allow instancing
      #pragma multi_compile_instancing

      //shader functions
      #pragma vertex vert
      #pragma fragment frag

      //use unity shader library
      #include "UnityCG.cginc"

      //per vertex data that comes from the model/parameters
      struct appdata{
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      //per vertex data that gets passed from the vertex to the fragment function
      struct v2f{
        float4 position : SV_POSITION;
        half2 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        
      };

      UNITY_INSTANCING_BUFFER_START(Props)
      UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
      UNITY_INSTANCING_BUFFER_END(Props)

      sampler2D _MainTex;
      float4 _MainTex_ST;

      v2f vert(appdata v){
        v2f o;

        //setup instance id
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);

        //calculate the position in clip space to render the object
        o.position = UnityObjectToClipPos(v.vertex);
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        return o;
      }

      fixed4 frag(v2f i) : SV_TARGET{
          //setup instance id
          UNITY_SETUP_INSTANCE_ID(i);
          //get _Color Property from buffer
          fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
          //Return the color the Object is rendered in
          return  tex2D(_MainTex, i.texcoord) * color;
      }

      ENDCG
    }
  }
}