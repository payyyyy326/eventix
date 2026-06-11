"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useState } from "react";
import { authApi } from "../api/auth.api";
import { getErrorMessage } from "@/shared/lib/error";

export default function VerifyOtpForm() {
    const router = useRouter();
    const searchParams = useSearchParams();

    const email = searchParams.get("email") ?? "";
    const [otpCode, setOtpCode] = useState("");
    const [loading, setLoading] = useState(false);
    const [resending, setResending] = useState(false);

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();

        try {
            setLoading(true);

            const res = await authApi.verifyOtp({
                email,
                otpCode,
            });

            alert(res.message);
            router.push("/login");
        } catch (error) {
            alert(getErrorMessage(error));
        } finally {
            setLoading(false);
        }
    }

    async function handleResendOtp() {
        try {
            setResending(true);

            const res = await authApi.resendOtp({ email });

            alert(res.message);
        } catch (error) {
            alert(getErrorMessage(error));
        } finally {
            setResending(false);
        }
    }

    return (
        <div className="w-full max-w-md rounded-2xl border border-white/10 bg-white/10 p-8 shadow-2xl backdrop-blur-xl">
            <div className="mb-8 text-center">
                <h1 className="text-3xl font-bold text-white">
                    Xác thực OTP
                </h1>

                <p className="mt-2 text-sm text-slate-300">
                    Nhập mã xác thực đã được gửi đến email của bạn
                </p>

                <p className="mt-3 rounded-xl border border-white/10 bg-slate-950/50 px-4 py-2 text-sm text-violet-200">
                    {email || "Không tìm thấy email"}
                </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
                <div>
                    <label className="mb-2 block text-sm font-medium text-slate-200">
                        Mã OTP
                    </label>

                    <input
                        className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-center text-lg tracking-[0.35em] text-white outline-none transition placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                        placeholder="000000"
                        value={otpCode}
                        onChange={(e) => setOtpCode(e.target.value)}
                    />
                </div>

                <button
                    disabled={loading || !email}
                    className="w-full rounded-xl bg-gradient-to-r from-violet-600 to-blue-600 py-3 font-semibold text-white shadow-lg shadow-violet-500/25 transition hover:from-violet-500 hover:to-blue-500 disabled:cursor-not-allowed disabled:opacity-60"
                >
                    {loading ? "Đang xác thực..." : "Xác thực"}
                </button>

                <button
                    type="button"
                    disabled={resending || !email}
                    onClick={handleResendOtp}
                    className="w-full rounded-xl border border-white/10 bg-white/5 py-3 font-medium text-slate-200 transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-60"
                >
                    {resending ? "Đang gửi lại..." : "Gửi lại mã OTP"}
                </button>
            </form>

            <p className="mt-6 text-center text-sm text-slate-400">
                Nhập sai email?{" "}
                <a
                    href="/register"
                    className="font-medium text-violet-300 hover:text-violet-200"
                >
                    Đăng ký lại
                </a>
            </p>
        </div>
    );
}