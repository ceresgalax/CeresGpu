using System;
using System.Collections.Generic;
using System.Threading;
using CeresGL;

namespace CeresGpu.Graphics.OpenGL
{
    public class GLContext : IGLProvider
    {
        private readonly GL _gl;

        private readonly object _finalizerActionLock = new();
        private List<Action<GL>> _finalizerActions = new();
        private List<Action<GL>> _nextFinalizerActions = new();
        private readonly Thread _thread;
        
        public GLContext(GL gl, Thread thread)
        {
            _gl = gl;
            _thread = thread;
        }

        public GL Gl 
        {
            get {
                CheckThread();
                return _gl;
            }
        }

        public void AddFinalizerAction(Action<GL> action)
        {
            lock (_finalizerActionLock) {
                _nextFinalizerActions.Add(action);
            }
        }

        /// <summary>
        /// Allows us to perform finalizer cleanup on this GL Context's thread.
        /// </summary>
        public void ProcessFinalizerActions()
        {
            CheckThread();
            
            lock (_finalizerActionLock) {
                (_finalizerActions, _nextFinalizerActions) = (_nextFinalizerActions, _finalizerActions);
            }

            bool somethingBadHappened = false;
            
            foreach (Action<GL> action in _finalizerActions) {
                try {
                    action(_gl);
                } catch (Exception e) {
                    somethingBadHappened = true;
                    Console.Error.WriteLine(e);
                }
            }
            _finalizerActions.Clear();

            if (somethingBadHappened) {
                throw new InvalidOperationException(
                    "Exception thrown in one or more finalizer action. There may now be a memory leak. " +
                    "Best thing to do now is to let the application crash.");
            }
        }

        private void CheckThread()
        {
            if (Thread.CurrentThread != _thread) {
                throw new InvalidOperationException("This operation must be performed on the context's thread");
            }
        }

        public bool IsCurrentThreadContextThread => _thread == Thread.CurrentThread;

        public void DoOnContextThread(Action<GL> action)
        {
            if (IsCurrentThreadContextThread) {
                action(_gl);
                return;
            }
            
            object semaphore = new();
            lock (semaphore) {
                Action<GL> wrapper = gl => {
                    lock (semaphore) {
                        action(gl);
                        Monitor.Pulse(semaphore);
                    }
                };
                AddFinalizerAction(wrapper);
                Monitor.Wait(semaphore);
            }
        }
    }
}