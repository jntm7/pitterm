#pragma once

#include <any>
#include <string>
#include <unordered_map>

namespace pitterm::state {

// In-memory cache for API responses (avoids re-fetching)
class Cache {
public:
    template<typename T>
    void set(const std::string& key, const T& value) {
        cache_[key] = value;
    }

    template<typename T>
    bool get(const std::string& key, T& out_value) const {
        auto it = cache_.find(key);
        if (it != cache_.end()) {
            try {
                out_value = std::any_cast<T>(it->second);
                return true;
            } catch (const std::bad_any_cast&) {
                return false;
            }
        }
        return false;
    }

    bool contains(const std::string& key) const {
        return cache_.find(key) != cache_.end();
    }

    void clear() {
        cache_.clear();
    }

private:
    std::unordered_map<std::string, std::any> cache_;
};

}
