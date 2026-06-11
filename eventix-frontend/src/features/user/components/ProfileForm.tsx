"use client";

import { useEffect, useMemo, useState } from "react";
import { userApi } from "../api/user.api";
import { getErrorMessage } from "@/shared/lib/error";
import { authStorage } from "@/shared/lib/auth-storage";
import { getImageUrl } from "@/shared/lib/image-url";
import type { UserProfileResponse } from "../types/user.type";

export default function ProfileForm() {
    const [profile, setProfile] = useState<UserProfileResponse | null>(null);

    const [fullName, setFullName] = useState("");
    const [phoneNumber, setPhoneNumber] = useState("");
    const [avatar, setAvatar] = useState<File | null>(null);

    const [loading, setLoading] = useState(false);
    const [fetching, setFetching] = useState(true);

    useEffect(() => {
        async function fetchProfile() {
            try {
                const res = await userApi.getProfile();

                setProfile(res.data);
                setFullName(res.data.fullName ?? "");
                setPhoneNumber(res.data.phoneNumber ?? "");
            } catch (error) {
                alert(getErrorMessage(error));
            } finally {
                setFetching(false);
            }
        }

        fetchProfile();
    }, []);

    const avatarPreview = useMemo(() => {
        if (avatar) {
            return URL.createObjectURL(avatar);
        }

        return getImageUrl(profile?.avatarUrl);
    }, [avatar, profile?.avatarUrl]);

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();

        try {
            setLoading(true);

            const res = await userApi.updateProfile({
                fullName,
                phoneNumber,
                avatar,
            });

            setProfile(res.data);
            setAvatar(null);

            authStorage.setAuth(
                authStorage.getToken()!,
                authStorage.getRefreshToken()!,
                {
                    ...authStorage.getUser(),
                    fullName: res.data.fullName,
                    phoneNumber: res.data.phoneNumber,
                    avatarUrl: res.data.avatarUrl,
                }
            );

            alert(res.message);
            window.location.reload();
        } catch (error) {
            alert(getErrorMessage(error));
        } finally {
            setLoading(false);
        }
    }

    if (fetching) {
        return (
            <div className="mx-auto max-w-3xl rounded-2xl border border-white/10 bg-white/10 p-8 text-white">
                Đang tải hồ sơ...
            </div>
        );
    }

    if (!profile) {
        return (
            <div className="mx-auto max-w-3xl rounded-2xl border border-white/10 bg-white/10 p-8 text-white">
                Không tìm thấy hồ sơ.
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-4xl">
            <div className="mb-8">
                <h1 className="text-3xl font-bold text-white">Hồ sơ cá nhân</h1>
                <p className="mt-2 text-slate-400">
                    Quản lý thông tin tài khoản và ảnh đại diện của bạn.
                </p>
            </div>

            <form
                onSubmit={handleSubmit}
                className="grid gap-6 rounded-3xl border border-white/10 bg-white/10 p-8 shadow-2xl backdrop-blur-xl md:grid-cols-[260px_1fr]"
            >
                <div className="rounded-2xl border border-white/10 bg-slate-950/50 p-6 text-center">
                    <div className="mx-auto h-32 w-32 overflow-hidden rounded-full border border-white/10 bg-slate-900">
                        {avatarPreview ? (
                            <img
                                src={avatarPreview}
                                alt={profile.fullName || "Avatar"}
                                className="h-full w-full object-cover"
                            />
                        ) : (
                            <div className="flex h-full w-full items-center justify-center bg-gradient-to-r from-violet-600 to-blue-600 text-5xl font-bold text-white">
                                {(profile.fullName || profile.email)?.charAt(0).toUpperCase()}
                            </div>
                        )}
                    </div>

                    <label className="mt-5 block cursor-pointer rounded-xl border border-white/10 bg-white/5 px-4 py-3 text-sm font-medium text-slate-200 transition hover:bg-white/10">
                        Chọn ảnh mới
                        <input
                            type="file"
                            accept="image/*"
                            className="hidden"
                            onChange={(e) => setAvatar(e.target.files?.[0] ?? null)}
                        />
                    </label>

                    {avatar && (
                        <p className="mt-3 break-all text-xs text-slate-400">
                            {avatar.name}
                        </p>
                    )}

                    <div className="mt-6 border-t border-white/10 pt-5 text-left text-sm">
                        <p className="text-slate-400">Email</p>
                        <p className="mt-1 break-all font-medium text-white">
                            {profile.email}
                        </p>

                        <p className="mt-4 text-slate-400">Trạng thái</p>
                        <p className="mt-1 font-medium text-emerald-300">
                            {profile.emailVerified ? "Đã xác thực email" : "Chưa xác thực"}
                        </p>
                    </div>
                </div>

                <div className="space-y-5">
                    <div>
                        <label className="mb-2 block text-sm font-medium text-slate-200">
                            Họ tên
                        </label>
                        <input
                            value={fullName}
                            onChange={(e) => setFullName(e.target.value)}
                            className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none transition placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                            placeholder="Nhập họ tên"
                        />
                    </div>

                    <div>
                        <label className="mb-2 block text-sm font-medium text-slate-200">
                            Số điện thoại
                        </label>
                        <input
                            value={phoneNumber}
                            onChange={(e) => setPhoneNumber(e.target.value)}
                            className="w-full rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3 text-white outline-none transition placeholder:text-slate-500 focus:border-violet-400 focus:ring-2 focus:ring-violet-500/30"
                            placeholder="Nhập số điện thoại"
                        />
                    </div>

                    <div className="grid gap-4 rounded-2xl border border-white/10 bg-slate-950/40 p-5 text-sm text-slate-300 md:grid-cols-2">
                        <div>
                            <p className="text-slate-500">Ngày tạo</p>
                            <p className="mt-1 text-white">
                                {new Date(profile.createdAt).toLocaleString("vi-VN")}
                            </p>
                        </div>

                        <div>
                            <p className="text-slate-500">Cập nhật gần nhất</p>
                            <p className="mt-1 text-white">
                                {profile.updatedAt
                                    ? new Date(profile.updatedAt).toLocaleString("vi-VN")
                                    : "Chưa cập nhật"}
                            </p>
                        </div>
                    </div>

                    <div className="flex justify-end gap-3 pt-4">
                        <button
                            type="button"
                            onClick={() => {
                                setFullName(profile.fullName ?? "");
                                setPhoneNumber(profile.phoneNumber ?? "");
                                setAvatar(null);
                            }}
                            className="rounded-xl border border-white/10 px-5 py-3 text-sm font-medium text-slate-200 hover:bg-white/10"
                        >
                            Hủy thay đổi
                        </button>

                        <button
                            disabled={loading}
                            className="rounded-xl bg-gradient-to-r from-violet-600 to-blue-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-violet-500/25 transition hover:from-violet-500 hover:to-blue-500 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                            {loading ? "Đang lưu..." : "Lưu thay đổi"}
                        </button>
                    </div>
                </div>
            </form>
        </div>
    );
}