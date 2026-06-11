"use client";

import { useState } from "react";
import { userApi } from "../api/user.api";
import { getErrorMessage } from "@/shared/lib/error";

export default function ChangePasswordForm() {
    const [oldPassword, setOldPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();

        try {
            setLoading(true);

            const res = await userApi.changePassword({
                oldPassword,
                newPassword,
                confirmPassword,
            });

            alert(res.message);

            setOldPassword("");
            setNewPassword("");
            setConfirmPassword("");
        } catch (error) {
            alert(getErrorMessage(error));
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="mx-auto max-w-3xl">
            <div className="mb-8">
                <h1 className="text-3xl font-bold text-white">Đổi mật khẩu</h1>
                <p className="mt-2 text-slate-400">
                    Cập nhật mật khẩu định kỳ để bảo vệ tài khoản Eventix của bạn.
                </p>
            </div>

            <form
                onSubmit={handleSubmit}
                className="rounded-3xl border border-white/10 bg-white/10 p-8 shadow-2xl backdrop-blur-xl"
            >
                <div className="mb-6 rounded-2xl border border-violet-400/20 bg-violet-500/10 p-4 text-sm text-violet-100">
                    Mật khẩu mới nên có tối thiểu 6 ký tự và không trùng với mật khẩu cũ.
                </div>

                <div className="space-y-5">
                    <div>
                        <label className="mb-2 block text-sm font-medium text-slate-200">
                            Mật khẩu hiện tại
                        </label>
                        <input
                            type="password"
                            placeholder="Nhập mật khẩu hiện tại"
                            value={oldPassword}
                            onChange={(e) => setOldPassword(e.target.value)}
                            className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none transition placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                        />
                    </div>

                    <div>
                        <label className="mb-2 block text-sm font-medium text-slate-200">
                            Mật khẩu mới
                        </label>
                        <input
                            type="password"
                            placeholder="Nhập mật khẩu mới"
                            value={newPassword}
                            onChange={(e) => setNewPassword(e.target.value)}
                            className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none transition placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                        />
                    </div>

                    <div>
                        <label className="mb-2 block text-sm font-medium text-slate-200">
                            Xác nhận mật khẩu mới
                        </label>
                        <input
                            type="password"
                            placeholder="Nhập lại mật khẩu mới"
                            value={confirmPassword}
                            onChange={(e) => setConfirmPassword(e.target.value)}
                            className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none transition placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                        />
                    </div>
                </div>

                <div className="mt-8 flex justify-end gap-3">
                    <button
                        type="button"
                        onClick={() => {
                            setOldPassword("");
                            setNewPassword("");
                            setConfirmPassword("");
                        }}
                        className="rounded-xl border border-white/10 px-5 py-3 text-sm font-medium text-slate-200 hover:bg-white/10"
                    >
                        Hủy
                    </button>

                    <button
                        disabled={loading}
                        className="rounded-xl bg-gradient-to-r from-violet-600 to-blue-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-violet-500/25 transition hover:from-violet-500 hover:to-blue-500 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {loading ? "Đang cập nhật..." : "Cập nhật mật khẩu"}
                    </button>
                </div>
            </form>
        </div>
    );
}