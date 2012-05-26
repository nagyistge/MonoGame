// #region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
// #endregion License
// 
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;


namespace Microsoft.Xna.Framework.Graphics
{
    public class Effect : GraphicsResource
    {
		internal const string VARYING = @"
			varying vec2 v_TexCoord;
			varying vec4 v_Color;
			";
		
		internal const string DEFAULT_VERTEX = VARYING + @"
		    uniform mat4 u_projection;
			uniform mat4 u_modelview;
		
		    //attribute vec2 a_Position;
		    //attribute vec4 a_Color;
		    //attribute vec2 a_TexCoord;
		
		    void main()
		    {
		        vec4 pos = gl_Vertex; //vec4(a_Position.x, a_Position.y, 1, 1);
		        gl_Position =  u_projection * u_modelview * pos;
		        v_TexCoord = gl_MultiTexCoord0.xy; //a_TexCoord;
		        v_Color = gl_Color; //a_Color;
		    }";
		
		internal const string DEFAULT_FRAGMENT = VARYING + @"
			uniform sampler2D s_texture;
			void main()
			{
			    vec4 color = texture2D( s_texture, v_TexCoord );
			    color *= clamp(v_Color, 0.0, 1.0);
			    gl_FragColor = color;
			}";

        public EffectParameterCollection Parameters { get; set; }

        public EffectTechniqueCollection Techniques { get; set; }

        internal List<EffectParameter> _textureMappings = new List<EffectParameter>();

        private int fragment_handle;
        private int vertex_handle;
        private bool fragment;
        private bool vertex;
        private string filename;

        internal List<int> vertexShaders = new List<int>();
        internal List<int> fragmentShaders = new List<int>();

		public Effect( GraphicsDevice graphicsDevice, byte[] effectCode, CompilerOptions options )
			:this( graphicsDevice )
        {

			using( MemoryStream buffer = new MemoryStream( effectCode, false ) )
			using( BinaryReader reader = new BinaryReader( buffer ) )
            {
				string fragmentSource = DEFAULT_FRAGMENT;
				int fragmentblocklength = reader.ReadInt32();
				if (fragmentblocklength != 0) 
            {
					byte[] fragData = new byte[fragmentblocklength];

					int count = reader.Read( fragData, 0, fragmentblocklength );
					System.Diagnostics.Debug.Assert( count == fragmentblocklength );

					fragmentSource = Encoding.UTF8.GetString(fragData);
            }
				fragment_handle = CreateFragmentShaderFromSource( fragmentSource );

				string vertexSource = DEFAULT_VERTEX;
				int vertexblocklength = reader.ReadInt32();
            if (vertexblocklength != 0)
            {
					byte[] vertexData = new byte[vertexblocklength];

					int count = reader.Read( vertexData, 0, vertexblocklength );
					System.Diagnostics.Debug.Assert( count == vertexblocklength );

					vertexSource = Encoding.UTF8.GetString(vertexData);
            }

				vertex_handle = CreateVertexShaderFromSource( vertexSource );
            }

			DefineTechnique ("Technique1", "Pass1", 0, 0);
			CurrentTechnique = Techniques ["Technique1"];

        }

        protected Effect(GraphicsDevice graphicsDevice, Effect cloneSource)
        {
			if (graphicsDevice == null) {
                throw new ArgumentNullException("Graphics Device Cannot Be Null");
            }
            this.graphicsDevice = graphicsDevice;
        }

        protected Effect(GraphicsDevice graphicsDevice)
        {
			if (graphicsDevice == null) {
                throw new ArgumentNullException("Graphics Device Cannot Be Null");
            }
            this.graphicsDevice = graphicsDevice;
            Techniques = new EffectTechniqueCollection();
            Parameters = new EffectParameterCollection();
        }

		internal Effect (GraphicsDevice aGraphicsDevice, string aFileName) : this(aGraphicsDevice)
        {
            StreamReader streamReader = new StreamReader(aFileName);
            string text = streamReader.ReadToEnd();
            streamReader.Close();

            if (aFileName.ToLower().Contains("fsh"))
            {
                CreateFragmentShaderFromSource(text);
            }
            else
            {
                CreateVertexShaderFromSource(text);
            }

            DefineTechnique("Technique1", "Pass1", 0, 0);
            CurrentTechnique = Techniques["Technique1"];
        }

        protected Effect(Effect cloneSource)
        {

        }

		public Effect( GraphicsDevice graphicsDevice, byte[] effectCode)
			: this( graphicsDevice, effectCode, CompilerOptions.None )
		{ }

        public virtual Effect Clone(GraphicsDevice device)
        {
            Effect f = new Effect(graphicsDevice, this);
            return f;
        }

        public virtual Effect Clone()
        {
            return Clone(graphicsDevice);
        }

        internal static string Normalize(string FileName)
        {
            if (File.Exists(FileName))
                return FileName;

            // Check the file extension
			if (!string.IsNullOrEmpty (Path.GetExtension (FileName))) {
                return null;
            }

            // Concat the file name with valid extensions
            if (File.Exists(FileName + ".fsh"))
                return FileName + ".fsh";
            if (File.Exists(FileName + ".vsh"))
                return FileName + ".vsh";

            return null;
        }

		public EffectTechnique CurrentTechnique { 
            get;
            set;
        }

        protected internal virtual void OnApply()
        {

            Viewport viewport = GraphicsDevice.Viewport;
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, -1, 1);
			GL.UniformMatrix4( Parameters["u_projection"].UniformLocation, false, ref projection );
        }
		
		protected int CompileShaderFromSource( string source, ShaderType shaderType )
		{
			int shader = GL.CreateShader( shaderType );
			GL.ShaderSource( shader, source );
			GL.CompileShader( shader );
			
			int compiled = 0;
			GL.GetShader( shader, ShaderParameter.CompileStatus, out compiled );
			if ( compiled == (int)All.False )
			{
				string log = GL.GetShaderInfoLog( shader );
				if( log != "" )
				{
					Console.WriteLine("{0} Shader compilation failed:\n{1}", shaderType == ShaderType.VertexShader ? "Vertex" : "Fragment", log );
				}
				throw new Exception("Shader error");
			}
			return shader;
		}

		protected int CreateVertexShaderFromSource(string source)
        {
			int shader = CompileShaderFromSource(source, ShaderType.VertexShader );
			vertexShaders.Add( shader );
			return shader;
        }


		protected int CreateFragmentShaderFromSource(string source)
        {
			int shader = CompileShaderFromSource(source, ShaderType.FragmentShader );
			fragmentShaders.Add( shader );
			return shader;
        }

        protected void DefineTechnique(string techniqueName, string passName, int vertexIndex, int fragmentIndex)
        {
            EffectTechnique tech = new EffectTechnique(this);
            tech.Name = techniqueName;
            EffectPass pass = new EffectPass(tech);
            pass.Name = passName;
            pass.VertexIndex = vertexIndex;
            pass.FragmentIndex = fragmentIndex;
            pass.ApplyPass();
            tech.Passes._passes.Add(pass);
            Techniques._techniques.Add(tech);
            LogShaderParameters(String.Format("Technique {0} - Pass {1} :", tech.Name, pass.Name), pass.shaderProgram);

		}

		protected void UpdateTechnique (string techniqueName, string passName, int vertexIndex, int fragmentIndex)
		{
			EffectTechnique tech = Techniques[techniqueName];
			EffectPass pass = tech.Passes[passName];
			//pass.VertexIndex = vertexIndex;
			//pass.FragmentIndex = fragmentIndex;
			pass.UpdatePass(vertexIndex, fragmentIndex);
			LogShaderParameters(String.Format("Technique {0} - Pass {1} :" ,tech.Name ,pass.Name), pass.shaderProgram);
        }
		
		private int GetUniformUserIndex (string uniformName)
		{
			int sPos = uniformName.LastIndexOf ("_s");
			int index;

			// if there's no such construct on the string or it's not followed by numbers only
			if (sPos == -1 || !int.TryParse (uniformName.Substring (sPos + 2), out index))
				return -1; // no user index

			return index;
		}

        // Output the log of an object
        private void LogShaderParameters(string whichObj, int obj)
        {
            int actUnis = 0;
            Parameters._parameters.Clear();

            GL.GetProgram(obj, ProgramParameter.ActiveUniforms, out actUnis);

            Console.WriteLine("{0} {1}", whichObj, actUnis);

            int size;
            ActiveUniformType type;
            StringBuilder name;
            int length;

			for (int x =0; x < actUnis; x++) {
				int uniformLocation, userIndex;
				string uniformName;

                name = new StringBuilder(100);
                GL.GetActiveUniform(obj, x, 100, out length, out size, out type, name);
				uniformName = name.ToString();
				userIndex = GetUniformUserIndex(uniformName);
				uniformLocation = GL.GetUniformLocation(obj, uniformName);

                Console.WriteLine("{0}: {1} {2} {3} {4}", uniformLocation, uniformName, type, length, size);
				EffectParameter efp = new EffectParameter (this, uniformName, uniformLocation, userIndex, uniformLocation,
				                                          type.ToString (), length, size);
                Parameters._parameters.Add(efp.Name, efp);
				if (efp.ParameterType == EffectParameterType.Texture2D) {
                    _textureMappings.Add(efp);
                }
            }

        }
    }
}