using System;
using StereoKit;
using System.Collections.Generic;
using StereoKit.Framework;

namespace My.Framework
{
	/// <summary>This enum specifies how HandMenuItems should behave
	/// when selected! This is in addition to the HandMenuItem's 
	/// callback function.</summary>

	/// <summary>A menu that shows up in circle around the user's 
	/// hand! Selecting an item can perform an action, or even spawn
	/// a sub-layer of menu items. This is an easy way to store 
	/// actions out of the way, yet remain easily accessible to the
	/// user.
	/// 
	/// The user faces their palm towards their head, and then makes
	/// a grip motion to spawn the menu. The user can then perform actions
	/// by making fast, direction based motions that are easy to build 
	/// muscle memory for.</summary>
	public class HandMenuRadial : IStepper
	{
		#region Fields
		const float minDist = 0.03f;
		const float midDist = 0.065f;
		const float maxDist = 0.1f;
		const float minScale = 0.05f;

		public bool active = false;
		Pose menuPose;
		Pose destPose;

		public TextStyle textStyle = TextStyle.Default;
		public TextStyle iconStyle = TextStyle.Default;

		public HandRadialLayer[] layers;
		Stack<int> navStack = new Stack<int>();
		public int activeLayer;
		LinePoint[] circle;
		LinePoint[] innerCircle;
		float activation = minScale;
		float angleOffset = 0;

		#endregion

		#region Public Methods

		/// <summary>Creates a hand menu from the provided array of menu 
		/// layers! HandMenuRadial is an IStepper, so proper usage is to
		/// add it to the Stepper list via `StereoKitApp.AddStepper`.</summary>
		/// <param name="menuLayers">Starting layer is always the first one
		/// in the list! Layer names in the menu items refer to layer names
		/// in this list.</param>
		public HandMenuRadial(params HandRadialLayer[] menuLayers)
		{
			layers = menuLayers;
			circle = new LinePoint[48];
			innerCircle = new LinePoint[circle.Length];

			// Pre-generate circles for the menu
			float step = 360.0f / (circle.Length - 1);
			Color color = Color.White * 0.5f;
			for (int i = 0; i < circle.Length; i++)
			{
				Vec3 dir = Vec3.AngleXY(i * step);
				circle[i] = new LinePoint(dir * maxDist, color, 2 * Units.mm2m);
				innerCircle[i] = new LinePoint(dir * minDist, color, 2 * Units.mm2m);
			}
		}

		/// <summary>Force the hand menu to show at a specific location.
		/// This will close the hand menu if it was already open, and resets
		/// it to the root menu layer. Also plays an opening sound.</summary>
		/// <param name="at">A world space position for the hand menu.</param>
		public void Show(Vec3 at)
		{
			if (active)
				Close();
			Default.SoundClick.Play(at);
			destPose.position = at;
			destPose.orientation = Quat.LookAt(menuPose.position, Input.Head.position);
			//activeLayer = 0;
			active = true;
		}

		/// <summary>Closes the menu if it's open! Plays a closing sound.</summary>
		public void Close()
		{
			if (!active)
				return;
			Default.SoundUnclick.Play(menuPose.position);
			activation = minScale;
			active = false;
			angleOffset = 0;
			Main.MenuClosed();
			//navStack.Clear();
		}

		public void Reset()
		{
			Close();
			navStack.Clear();
			activeLayer = 0;
		}

		/// <summary>Part of IStepper, you shouldn't be calling this yourself.</summary>
		public void Step()
		{
			try
			{
				
				Hand hand = Input.Hand(Handed.Right);
				bool facing = HandFacingHead(hand);

				//Log.Info(facing + ":" + active);

				if (facing && !active)
					StepMenuIndicator(hand);

				if (active)
					StepMenu(hand);
			} catch (Exception ex)
            {
				Log.Err(ex.Source + ":" + ex.Message);
            }
		}

		/// <summary>HandMenuRadial is always Enabled.</summary>
		public bool Enabled => true;
		/// <summary>Part of IStepper, you shouldn't be calling this yourself.</summary>
		/// <returns>Always returns true.</returns>
		public bool Initialize() => true;
		/// <summary>Part of IStepper, you shouldn't be calling this yourself.</summary>
		public void Shutdown() { }

		#endregion

		#region Private Methods

		void StepMenuIndicator(Hand hand)
		{
			if (Platform.FilePickerVisible)
				return;

			// Scale the indicator based on the 'activation' of the grip motion
			activation = Math.Max(0.2f, hand.gripActivation * .3f);

			// Show the indicator towards the middle of the fingers that control
			// the grip motion, the help show the user 
			menuPose.position = hand.palm.position;
				//(hand[FingerId.Ring, JointId.Tip].position +
				//hand[FingerId.Ring, JointId.Root].position) * 0.5f;
			menuPose.orientation = Quat.LookAt(menuPose.position, Input.Head.position);
			menuPose.position += menuPose.Forward * U.cm * 1.5f;

			bool aligned = Vec3.Dot(hand.palm.orientation * Vec3.Forward, (hand.palm.position - Input.Head.position).Normalized) < -.99f;

			// Draw the menu circle!
			Hierarchy.Push(menuPose.ToMatrix(activation));
			Lines.Add(circle);
			Hierarchy.Push(Matrix.S(1.6f));
			Text.Add(aligned?"Grip":"Align", Matrix.Identity,TextAlign.Center);
			Hierarchy.Pop();
			Hierarchy.Pop();
			Hierarchy.Push(Matrix.TRS(hand.palm.position + (hand.palm.orientation * Vec3.Forward * .03f), hand.palm.orientation, .16f));
			Lines.Add(circle);
			Hierarchy.Pop();

			

			// And if the user grips, show the menu!
			if (hand.IsJustGripped && aligned)
				Show(hand[FingerId.Index, JointId.Tip].position);
		}

		private string[] bits;
		private const float menuScale = 1f;
		void StepMenu(Hand hand)
		{
			try
			{
				// Animate the menu a bit
				float time = (Time.Elapsedf * 24);
				menuPose.position = Vec3.Lerp(menuPose.position, destPose.position, time);
				menuPose.orientation = Quat.Slerp(menuPose.orientation, destPose.orientation, time);
				activation = SKMath.Lerp(activation, menuScale, time);

				// Pre-calculate some circle traversal values
				HandRadialLayer layer = layers[activeLayer];
				int count = layer.items.Length;

				float step = 360 / count;
				float halfStep = step / 2;

				// Push the Menu's pose onto the stack, so we can draw, and work
				// in local space.
				Hierarchy.Push(menuPose.ToMatrix(activation));

				// Calculate the status of the menu!
				Vec3 tipWorld = hand[FingerId.Index, JointId.Tip].position;
				Vec3 tipLocal = Hierarchy.ToLocal(tipWorld);
				float selectDist = minDist + ((maxDist - minDist) * .2f);

				float magSq = tipLocal.MagnitudeSq;
				bool onMenu = tipLocal.z > -0.02f && tipLocal.z < 0.02f;
				bool focused = onMenu && magSq > minDist * minDist;
				bool selected = onMenu && magSq > selectDist * selectDist;
				bool cancel = magSq > maxDist * maxDist;

				// Find where our finger is pointing to, and draw that
				float fingerAngle = (float)Math.Atan2(tipLocal.y, tipLocal.x) * Units.rad2deg - (layer.startAngle + angleOffset);
				while (fingerAngle < 0) fingerAngle += 360;
				int angleId = (int)(fingerAngle / step);
				float planeDistance = Math.Max(.01f, .05f - Math.Abs(tipLocal.z));

				Lines.Add(Vec3.Zero, new Vec3(tipLocal.x, tipLocal.y, 0), Color.White, 0.1f * planeDistance);

				// Draw the menu inner and outer circles
				Lines.Add(circle);
				Lines.Add(innerCircle);

				// Now draw each of the menu items!

				for (int i = 0; i < count; i++)
				{
					bits = layer.items[i].name.Split('|');
					bits[0] = bits[0].Replace("XXX", Main.actionRequested);

					float currAngle = i * step + layer.startAngle + angleOffset;
					bool highlightText = focused && angleId == i;
					bool highlightLine = highlightText || (focused && (angleId + 1) % count == i);
					Vec3 dir = Vec3.AngleXY(currAngle);
					Lines.Add(dir * minDist, dir * maxDist, highlightLine ? Color.White : Color.White * 0.5f, highlightLine ? 0.002f : 0.001f);
					Text.Add(bits[0], Matrix.TRS(Vec3.AngleXY(currAngle + halfStep) * (midDist + .015f), Quat.FromAngles(0, 0, currAngle + halfStep - 90), highlightText ? 1.2f : 1), textStyle, TextAlign.BottomCenter);

					if (bits.Length > 1)
						Text.Add(bits[1], Matrix.TRS(Vec3.AngleXY(currAngle + halfStep) * (midDist - .015f), Quat.FromAngles(0, 0, currAngle + halfStep - 90), highlightText ? 1.2f : 1), iconStyle, TextAlign.BottomCenter);

				}

				// Done with local work
				Hierarchy.Pop();

				// Execute any actions that were discovered

				// But not if we're still in the process of animating, interaction values 
				// could be pretty incorrect when we're still lerping around.
				if (activation < 0.95f) return;
				if (selected) SelectItem(angleId, layer.items[angleId], tipWorld, (angleId + 0.5f) * step);
				if (cancel) Close();
			}
			catch (Exception ex)
            {
				Log.Err(ex.Source + ":" + ex.Message);
			}
		}

		public void SelectLayer(string name)
		{
			Default.SoundClick.Play(menuPose.position);
			navStack.Push(activeLayer);
			activeLayer = Array.FindIndex(layers, l => l.layerName == name);
			if (activeLayer == -1)
				Log.Err($"Couldn't find hand menu layer named {name}!");
		}

		public void Back()
		{
			Default.SoundUnclick.Play(menuPose.position);
			if (navStack.Count > 0)
				activeLayer = navStack.Pop();
		}

		public int lastIdSelected;
		void SelectItem(int id, HandMenuItem item, Vec3 at, float fromAngle)
		{
			try
			{
				lastIdSelected = id;
				if (item.action == HandMenuAction.Close) Close();
				else if (item.action == HandMenuAction.Layer) SelectLayer(item.layerName);
				else if (item.action == HandMenuAction.Back) Back();

				if (item.action == HandMenuAction.Back || item.action == HandMenuAction.Layer)
					Reposition(at, fromAngle);

				item.callback?.Invoke();
			} catch (Exception ex)
            {
				Log.Err(ex.Source + ":" + ex.Message);
            }
		}

		void Reposition(Vec3 at, float fromAngle)
		{
			Plane plane = new Plane(menuPose.position, menuPose.Forward);
			destPose.position = plane.Closest(at);

			if (layers[activeLayer].backAngle != 0)
			{
				angleOffset = (fromAngle - layers[activeLayer].backAngle) + 180;
				while (angleOffset < 0) angleOffset += 360;
				while (angleOffset > 360) angleOffset -= 360;
			}
			else angleOffset = 0;
		}

		static bool HandFacingHead(Hand hand)
		{
			if (!hand.IsTracked)
				return false;

			Vec3 palmDirection = hand.palm.Forward.Normalized;
			Vec3 directionToHead = (Input.Head.position - hand.palm.position).Normalized;

			return Vec3.Dot(palmDirection, directionToHead) > 0.5f;
		}

		#endregion
	}
}
