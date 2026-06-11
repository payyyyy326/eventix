import ForgotPasswordForm from "@/features/auth/components/ForgotPasswordForm";

export default function ForgotPasswordPage() {
    return (
        <main className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top,_#312e81,_#020617_45%,_#000_100%)] px-4">
            <ForgotPasswordForm />
        </main>
    );
}