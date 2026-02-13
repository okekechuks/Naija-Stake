import Categories from "@/components/Categories";
import PopularBets from "@/components/PopularBets";

export default function HomePage() {
  return (
    <main className="max-w-7xl mx-auto px-4 py-8 space-y-10">
      <header className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">StakeIt</h1>
        <div className="text-sm text-slate-400">
          Balance: ₦120,000
        </div>
      </header>

      <PopularBets />

      <Categories />
    </main>
  );
}
