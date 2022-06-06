using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;
using UnhollowerBaseLib;

namespace TownOfHost
{
    public class CustomRpcSender
    {
        public MessageWriter writer;
        public SendOption sendOption;
        public bool isUnsafe;

        private State currentState = State.BeforeInit;

        private CustomRpcSender() { }
        public CustomRpcSender(SendOption sendOption, bool isUnsafe)
        {
            writer = MessageWriter.Get(sendOption);

            this.sendOption = sendOption;
            this.isUnsafe = isUnsafe;

            currentState = State.Ready;
        }
        public static CustomRpcSender Create(SendOption sendOption = SendOption.None, bool isUnsafe = false)
        {
            return new CustomRpcSender(sendOption, isUnsafe);
        }

        public MessageWriter StartRpc(
          uint targetNetId,
          byte callId,
          int targetClientId = -1)
        {
            if (currentState != State.Ready && !isUnsafe)
            {
                Logger.Error("RPCを開始しようとしましたが、StateがReady(準備完了)ではありません", "CustomRpcSender.Error");
                return null;
            }

            if (targetClientId < 0)
            {
                // 全員に対するRPC
                writer.StartMessage(5);
                writer.Write(AmongUsClient.Instance.GameId);
            }
            else
            {
                // 特定のクライアントに対するRPC (Desync)
                writer.StartMessage(6);
                writer.Write(AmongUsClient.Instance.GameId);
                writer.WritePacked(targetClientId);
            }
            writer.StartMessage(2);
            writer.WritePacked(targetNetId);
            writer.Write(callId);

            currentState = State.Writing;
            return writer;
        }
        public void EndRpc()
        {
            if (currentState != State.Writing && !isUnsafe)
            {
                Logger.Error("RPCを終了しようとしましたが、StateがWriting(書き込み中)ではありません", "CustomRpcSender.Error");
                return;
            }

            writer.EndMessage();
            writer.EndMessage();
            currentState = State.Ready;
        }
        public void SendMessage()
        {
            if (currentState != State.Ready && !isUnsafe)
            {
                Logger.Error("RPCを終了しようとしましたが、StateがReady(準備完了)ではありません", "CustomRpcSender.Error");
                return;
            }

            AmongUsClient.Instance.SendOrDisconnect(writer);
            currentState = State.Finished;
            writer.Recycle();
        }

        // Write
        public void Write(MessageWriter msg, bool includeHeader) => Write(w => w.Write(msg, includeHeader));
        public void Write(float val) => Write(w => w.Write(val));
        public void Write(string val) => Write(w => w.Write(val));
        public void Write(ulong val) => Write(w => w.Write(val));
        public void Write(int val) => Write(w => w.Write(val));
        public void Write(uint val) => Write(w => w.Write(val));
        public void Write(ushort val) => Write(w => w.Write(val));
        public void Write(byte val) => Write(w => w.Write(val));
        public void Write(sbyte val) => Write(w => w.Write(val));
        public void Write(bool val) => Write(w => w.Write(val));
        public void Write(Il2CppStructArray<byte> bytes) => Write(w => w.Write(bytes));
        public void Write(Il2CppStructArray<byte> bytes, int offset, int length) => Write(w => w.Write(bytes, offset, length));
        public void WriteBytesAndSize(Il2CppStructArray<byte> bytes) => Write(w => w.WriteBytesAndSize(bytes));
        public void WritePacked(int val) => Write(w => w.WritePacked(val));
        public void WritePacked(uint val) => Write(w => w.WritePacked(val));

        private void Write(Action<MessageWriter> action)
        {
            if (currentState != State.Writing && !isUnsafe)
            {
                Logger.Error("RPCを書き込もうとしましたが、StateがWrite(書き込み中)ではありません", "CustomRpcSender.Error");
                return;
            }

            action(writer);
        }

        public enum State
        {
            BeforeInit = 0,
            Ready,
            Writing,
            Finished,
        }
    }
}