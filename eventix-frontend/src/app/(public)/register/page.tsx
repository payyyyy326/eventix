import RegisterForm from "@/features/auth/components/RegisterForm";

export default function RegisterPage() {
    return (
        <main className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top,_#312e81,_#020617_45%,_#000_100%)] px-4 py-10">
            <RegisterForm />
        </main>
    );
}