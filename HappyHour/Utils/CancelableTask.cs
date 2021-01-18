using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HappyHour.Utils
{
    class CancelableTask
    {
        Task _runnintTask;
        CancellationTokenSource _tokenSrouce;
        Action _action;

        public CancelableTask(Action action)
        {
            _action = action;
            _tokenSrouce = new CancellationTokenSource();
        }

        public void Cancel()
        {
            _tokenSrouce.Cancel();
            _runnintTask.Wait();
        }

        public async void Run()
        {
            if (_runnintTask != null && !_runnintTask.IsCompleted)
            {
                _tokenSrouce.Cancel();
                _runnintTask.Wait();
            }
            var token = _tokenSrouce.Token;
            _runnintTask = Task.Run(_action);

            await _runnintTask;
        }
    }
}
